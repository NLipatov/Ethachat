﻿using Limp.Client.ClientOnlyModels;
using Limp.Client.ClientOnlyModels.ClientOnlyExtentions;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.Handlers.MessageDecryption;
using Limp.Client.Pages.Chat.Logic.MessageBuilder;
using Limp.Client.Services.CloudKeyService;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.HubServices.Extensions;
using Limp.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Limp.Client.Services.InboxService;
using Limp.Client.Services.JWTReader;
using Limp.Client.Services.UndeliveredMessagesStore;
using LimpShared.Encryption;
using LimpShared.Models.ConnectedUsersManaging;
using LimpShared.Models.Message;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly IMessageBox _messageBox;
        private readonly IJSRuntime _jSRuntime;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAESOfferHandler _aESOfferHandler;
        private readonly IUsersService _usersService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IUndeliveredMessagesRepository _undeliveredMessagesRepository;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IBrowserKeyStorage _browserKeyStorage;
        private readonly IMessageDecryptor _messageDecryptor;
        private string myName;
        public bool IsConnected() => hubConnection?.State == HubConnectionState.Connected;

        private HubConnection? hubConnection { get; set; }

        public MessageService
        (IMessageBox messageBox,
        IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        ICryptographyService cryptographyService,
        IAESOfferHandler aESOfferHandler,
        IUsersService usersService,
        ICallbackExecutor callbackExecutor,
        IUndeliveredMessagesRepository undeliveredMessagesRepository,
        IMessageBuilder messageBuilder,
        IBrowserKeyStorage browserKeyStorage,
        IMessageDecryptor messageDecryptor)
        {
            _messageBox = messageBox;
            _jSRuntime = jSRuntime;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _undeliveredMessagesRepository = undeliveredMessagesRepository;
            _messageBuilder = messageBuilder;
            _browserKeyStorage = browserKeyStorage;
            _messageDecryptor = messageDecryptor;
        }
        public async Task<HubConnection> GetHubConnectionAsync()
        {
            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                NavigationManager.NavigateTo("signin");
                return null;
            }

            //Loading from local storage earlier saved AES keys
            InMemoryKeyStorage.AESKeyStorage = (await _browserKeyStorage.ReadLocalKeyChainAsync())?.AESKeyStorage ?? new();

            if (hubConnection?.State == HubConnectionState.Connected)
            {
                return hubConnection;
            }
            else
            {
                if (hubConnection is null)
                {
                    InitializeHubConnection();
                    RegisterHubEventHandlers();
                }
                else
                {
                    await hubConnection.DisposeAsync();
                    InitializeHubConnection();
                    RegisterHubEventHandlers();
                }
            }

            if (hubConnection == null)
                throw new ArgumentException($"{nameof(MessageService)} {nameof(hubConnection)} initialization failed. {nameof(hubConnection)} is null.");

            await hubConnection.StartAsync();

            await hubConnection.SendAsync("SetUsername", accessToken);

            hubConnection.Closed += OnConnectionLost;

            return hubConnection;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            await Console.Out.WriteLineAsync("MessageHub connection lost. Reconnecting.");
            await GetHubConnectionAsync();
        }

        private void InitializeHubConnection()
        {
            hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/messageDispatcherHub"))
            .AddMessagePackProtocol()
            .Build();
        }

        private void RegisterHubEventHandlers()
        {
            if (hubConnection is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            hubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
            });

            hubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    if (hubConnection.State != HubConnectionState.Connected)
                    {
                        await hubConnection.StopAsync();
                        await hubConnection.StartAsync();
                    }

                    if (message.Type is MessageType.UserMessage)
                    {
                        if (string.IsNullOrWhiteSpace(message.Sender))
                            throw new ArgumentException($"Cannot get a message sender - {nameof(message.Sender)} contains empty string.");

                        string decryptedMessageText = string.Empty;
                        try
                        {
                            decryptedMessageText = await _messageDecryptor.DecryptAsync(message);
                        }
                        catch (Exception ex)
                        {
                            await Console.Out.WriteLineAsync("Cannot decrypt received user message. Regenerating AES key.");
                            await NegotiateOnAESAsync(message.Sender);
                        }

                        if (!string.IsNullOrWhiteSpace(decryptedMessageText))
                        {
                            ClientMessage clientMessage = message.AsClientMessage();
                            clientMessage.PlainText = decryptedMessageText;

                            await hubConnection.SendAsync("MessageReceived", message.Id, message.Sender);
                            await _messageBox.AddMessageAsync(clientMessage, false);
                        }
                        else
                        {
                            if (message.Sender is not null)
                                await NegotiateOnAESAsync(message.Sender);
                        }
                    }
                    else if (message.Type == MessageType.AesAccept)
                    {
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
                        _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        InMemoryKeyStorage.AESKeyStorage[message.Sender!].IsAccepted = true;
                        return;
                    }
                    else if (message.Type == MessageType.AesOffer)
                    {
                        if (hubConnection != null)
                        {
                            await hubConnection.SendAsync("Dispatch", await _aESOfferHandler.GetAESOfferResponse(message));
                            _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        }
                    }
                }

                //If we dont yet know a partner Public Key and we dont have an AES Key for chat with partner,
                //we will request it from server side.
                if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == message.Sender).Value == null
                &&
                _browserKeyStorage.GetAESKeyForChat(message.Sender!) == null)
                {
                    if (hubConnection == null)
                    {
                        await ReconnectAsync();
                    }
                    else
                        await hubConnection.SendAsync("GetAnRSAPublic", message.Sender);
                }
            });

            hubConnection.On<Guid>("OnReceiverMarkedMessageAsReceived", messageId =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnReceiverMarkedMessageAsReceived");
            });

            hubConnection.On<Guid>("MessageHasBeenRead", messageId =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnReceiverMarkedMessageAsRead");
            });

            //Handling server side response on partners Public Key
            hubConnection.On<string, string>("ReceivePublicKey", async (partnersUsername, partnersPublicKey) =>
            {
                if (partnersUsername == "You")
                    return;
                //Storing Public Key in our in-memory storage
                InMemoryKeyStorage.RSAKeyStorage.TryAdd(partnersUsername, new Key
                {
                    Type = KeyType.RsaPublic,
                    Contact = partnersUsername,
                    Format = KeyFormat.PemSpki,
                    Value = partnersPublicKey
                });

                //Now we can send an encrypted offer on AES Key
                //We will encrypt our offer with a partners RSA Public Key
                await RegenerateAESAsync(_cryptographyService!, partnersUsername, partnersPublicKey);
            });

            hubConnection.On<string>("OnMyNameResolve", async username =>
            {
                myName = username;
                string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    NavigationManager.NavigateTo("/signIn");
                    return;
                }
                if (string.IsNullOrWhiteSpace(InMemoryKeyStorage.MyRSAPublic?.Value?.ToString()))
                {
                    throw new ApplicationException("RSA Public key was not properly generated.");
                }

                await UpdateRSAPublicKeyAsync(accessToken, InMemoryKeyStorage.MyRSAPublic);
            });
        }

        private async Task<HubConnection?> TryGetExistingHubConnection()
        {
            if (hubConnection != null)
            {
                if (hubConnection.State != HubConnectionState.Connected)
                {
                    await hubConnection.StopAsync();
                    await hubConnection.StartAsync();
                }
                return hubConnection;
            }
            return null;
        }
        public async Task UpdateRSAPublicKeyAsync(string accessToken, Key RSAPublicKey)
        {
            if (!InMemoryKeyStorage.isPublicKeySet)
            {
                await _usersService.SetRSAPublicKey(accessToken, RSAPublicKey);
            }
        }
        private async Task RegenerateAESAsync
        (ICryptographyService cryptographyService,
        string partnersUsername,
        string partnersPublicKey)
        {
            await cryptographyService.GenerateAESKeyAsync(partnersUsername, async (aesKeyForConversation) =>
            {
                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.CreationDate = DateTime.UtcNow;
                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value = aesKeyForConversation;
                await _browserKeyStorage.SaveInMemoryKeysInLocalStorage();
                string? offeredAESKeyForConversation = InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value!.ToString();

                if (string.IsNullOrWhiteSpace(offeredAESKeyForConversation))
                    throw new ApplicationException("Could not properly generated an AES Key for conversation");

                //When this callback is called, AES key for conversation is already generated
                //We now need to encrypt this AES key and send it to partner
                string? encryptedAESKey = (await cryptographyService
                .EncryptAsync<RSAHandler>
                    (new Cryptogramm { Cyphertext = offeredAESKeyForConversation },
                    //We will encrypt it with partners Public Key, so he will be able to decrypt it with his Private Key
                    PublicKeyToEncryptWith: partnersPublicKey)).Cyphertext;

                if (string.IsNullOrWhiteSpace(encryptedAESKey))
                    throw new ApplicationException("Could not encrypt a AES Key, got empty string.");

                Message offerOnAES = new()
                {
                    Type = MessageType.AesOffer,
                    DateSent = DateTime.UtcNow,
                    Sender = TokenReader.GetUsernameFromAccessToken(await JWTHelper.GetAccessTokenAsync(_jSRuntime)),
                    TargetGroup = partnersUsername,
                    Cryptogramm = new()
                    {
                        Cyphertext = encryptedAESKey,
                    }
                };

                if (hubConnection?.State != HubConnectionState.Connected)
                {
                    if (hubConnection is not null)
                        await hubConnection.DisposeAsync();

                    await GetHubConnectionAsync();
                }

                await hubConnection!.SendAsync("Dispatch", offerOnAES);
            });
        }

        public async Task DisconnectAsync()
        {
            await hubConnection.DisconnectAsync();
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            if (hubConnection?.State != HubConnectionState.Connected)
                hubConnection = await GetHubConnectionAsync();

            await hubConnection.SendAsync("GetAnRSAPublic", partnerUsername);
        }

        public async Task ReconnectAsync()
        {
            await DisconnectAsync();
            await GetHubConnectionAsync();
        }

        public async Task SendMessage(Message message)
        {
            if (hubConnection != null)
            {
                await hubConnection.SendAsync("Dispatch", message);
            }
            else
            {
                await ReconnectAsync();
                await SendMessage(message);
            }
        }
        public async Task SendUserMessage(string text, string targetGroup, string myUsername)
        {
            Guid messageId = Guid.NewGuid();
            Message messageToSend = await _messageBuilder.BuildMessageToBeSend(text, targetGroup, myUsername, messageId);

            await AddAsUnreceived(text, targetGroup, myUsername, messageId);

            await AddToMessageBox(text, targetGroup, myUsername, messageId);

            await SendMessage(messageToSend);
        }

        private async Task AddToMessageBox(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _messageBox.AddMessageAsync(new ClientMessage
            {
                Id = messageId,
                Sender = myUsername,
                TargetGroup = targetGroup,
                PlainText = text,
                DateSent = DateTime.UtcNow
            },
            isEncrypted: false);
        }

        private async Task AddAsUnreceived(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _undeliveredMessagesRepository.AddAsync(new ClientMessage
            {
                Id = messageId,
                Sender = myUsername,
                TargetGroup = targetGroup,
                PlainText = text,
                DateSent = DateTime.UtcNow
            });
        }

        public async Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername)
        {
            if (messageSender == myUsername) //If it's our message, we don't want to notify partner that we've seen our message
                return;

            if (hubConnection is not null)
            {
                if (hubConnection.State is HubConnectionState.Connected)
                {
                    await hubConnection.SendAsync("MessageHasBeenRead", messageId, messageSender);
                }
            }

            throw new ArgumentException("Notification was not sent because hub connection is lost.");
        }
    }
}