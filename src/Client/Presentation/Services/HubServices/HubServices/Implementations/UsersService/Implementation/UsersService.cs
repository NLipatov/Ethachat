﻿using System.Collections.Concurrent;
using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Builders;
using Ethachat.Client.Services.UserIdentityService;
using EthachatShared.Constants;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Users;
using EthachatShared.Models.WebPushNotification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService.Implementation
{
    public class UsersService : IUsersService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IConfiguration _configuration;
        private readonly IKeyStorage _keyStorage;
        private bool _isConnectionClosedCallbackSet = false;
        private HubConnection? HubConnectionInstance { get; set; }

        private ConcurrentDictionary<Guid, Func<string, Task>> ConnectionIdReceivedCallbacks = new();
        private ConcurrentDictionary<Guid, Func<string, Task>> UsernameResolvedCallbacks = new();

        public UsersService
        (NavigationManager navigationManager,
            ICallbackExecutor callbackExecutor,
            IAuthenticationHandler authenticationHandler,
            IConfiguration configuration,
            IKeyStorage keyStorage)
        {
            NavigationManager = navigationManager;
            _callbackExecutor = callbackExecutor;
            _authenticationHandler = authenticationHandler;
            _configuration = configuration;
            _keyStorage = keyStorage;
            InitializeHubConnection();
            RegisterHubEventHandlers();
        }

        private void InitializeHubConnection()
        {
            if (HubConnectionInstance is not null)
                return;

            HubConnectionInstance = HubServiceConnectionBuilder
                .Build(NavigationManager.ToAbsoluteUri(HubRelativeAddresses.UsersHubRelativeAddress));
        }

        private void RegisterHubEventHandlers()
        {
            if (HubConnectionInstance is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            HubConnectionInstance.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            HubConnectionInstance.On<string>("ReceiveConnectionId",
                connectionId =>
                {
                    _callbackExecutor.ExecuteCallbackDictionary(connectionId, ConnectionIdReceivedCallbacks);
                });

            HubConnectionInstance.On<string>("OnNameResolve", async username =>
            {
                ActiveUserIdentity.SetUsername(username);
                
                _callbackExecutor.ExecuteSubscriptionsByName(username, "OnNameResolve");
                
                _callbackExecutor.ExecuteCallbackDictionary(username, UsernameResolvedCallbacks);

                await GetHubConnectionAsync();

                var rsaPublicKeys = await _keyStorage.GetAsync(string.Empty, KeyType.RsaPublic);
                await HubConnectionInstance.SendAsync("PostAnRSAPublic", username,
                    rsaPublicKeys.First().Value);
            });

            HubConnectionInstance.On<UserConnection>("IsUserOnlineResponse",
                (UserConnection) =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(UserConnection, "IsUserOnlineResponse");
                });

            HubConnectionInstance.On<NotificationSubscriptionDto[]>("ReceiveWebPushSubscriptions",
                subscriptions =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(subscriptions, "ReceiveWebPushSubscriptions");
                });

            HubConnectionInstance.On<NotificationSubscriptionDto[]>("RemovedFromWebPushSubscriptions",
                removedSubscriptions =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(removedSubscriptions,
                        "RemovedFromWebPushSubscriptions");
                });

            HubConnectionInstance.On("WebPushSubscriptionSetChanged",
                () => { _callbackExecutor.ExecuteSubscriptionsByName("WebPushSubscriptionSetChanged"); });

            HubConnectionInstance.On<IsUserExistDto>("UserExistanceResponse",
                async isUserExistDTO =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(isUserExistDTO, "UserExistanceResponse");
                });
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            if (!await _authenticationHandler.IsSetToUseAsync())
            {
                NavigationManager.NavigateTo("signin");
            }

            if (HubConnectionInstance == null)
                throw new ArgumentException($"{nameof(HubConnectionInstance)} was not properly instantiated.");

            while (HubConnectionInstance.State is HubConnectionState.Disconnected)
            {
                try
                {
                    await HubConnectionInstance.StartAsync();
                }
                catch
                {
                    var interval = int.Parse(_configuration["HubConnection:ReconnectionIntervalMs"] ?? "0");
                    await Task.Delay(interval);
                    return await GetHubConnectionAsync();
                }
            }
            
            _callbackExecutor.ExecuteSubscriptionsByName(true, "OnUsersHubConnectionStatusChanged");
            
            await HubConnectionInstance.SendAsync("SetUsername", await _authenticationHandler.GetCredentialsDto());

            if (_isConnectionClosedCallbackSet is false)
            {
                HubConnectionInstance.Closed += OnConnectionLost;
                _isConnectionClosedCallbackSet = true;
            }

            return HubConnectionInstance;
        }
        
        private async Task OnConnectionLost(Exception? exception)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(false, "OnUsersHubConnectionStatusChanged");
            await GetHubConnectionAsync();
        }

        public void RemoveConnectionIdReceived(Guid subscriptionId)
        {
            bool isRemoved = ConnectionIdReceivedCallbacks.Remove(subscriptionId, out _);
            if (!isRemoved)
            {
                RemoveConnectionIdReceived(subscriptionId);
            }
        }

        public Guid SubscribeToConnectionIdReceived(Func<string, Task> callback)
        {
            Guid subscriptionId = Guid.NewGuid();
            bool isAdded = ConnectionIdReceivedCallbacks.TryAdd(subscriptionId, callback);
            if (!isAdded)
            {
                SubscribeToConnectionIdReceived(callback);
            }

            return subscriptionId;
        }

        public Guid SubscribeToUsernameResolved(Func<string, Task> callback)
        {
            Guid subscriptionId = Guid.NewGuid();
            bool isAdded = UsernameResolvedCallbacks.TryAdd(subscriptionId, callback);
            if (!isAdded)
            {
                SubscribeToUsernameResolved(callback);
            }

            return subscriptionId;
        }

        public void RemoveUsernameResolved(Guid subscriptionId)
        {
            bool isRemoved = UsernameResolvedCallbacks.Remove(subscriptionId, out _);
            if (!isRemoved)
            {
                RemoveUsernameResolved(subscriptionId);
            }
        }

        public async Task SetRSAPublicKey(Key RSAPublicKey)
        {
            var credentials = await _authenticationHandler.GetCredentials();
            if (credentials is WebAuthnPair)
            {
                var hubConnection = await GetHubConnectionAsync();
                await hubConnection.SendAsync("SetRSAPublicKey", RSAPublicKey, credentials, null);
            }
            else if (credentials is JwtPair)
            {
                var hubConnection = await GetHubConnectionAsync();
                await hubConnection.SendAsync("SetRSAPublicKey", RSAPublicKey, null, credentials);
            }
        }

        public async Task ActualizeConnectedUsersAsync()
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("PushOnlineUsersToClients");
        }

        public async Task CheckIfUserOnline(string username)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("IsUserOnline", username);
        }

        public async Task AddUserWebPushSubscription(NotificationSubscriptionDto subscriptionDTO)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("AddUserWebPushSubscription", subscriptionDTO);
        }

        public async Task GetUserWebPushSubscriptions(CredentialsDTO credentialsDto)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("GetUserWebPushSubscriptions", credentialsDto);
        }

        public async Task RemoveUserWebPushSubscriptions(NotificationSubscriptionDto[] subscriptionsToRemove)
        {
            if (subscriptionsToRemove.All(x => x.JwtPair is null && x.WebAuthnPair is null))
                throw new ArgumentException
                ($"At least one of parameters array " +
                 $"should have it's {nameof(NotificationSubscriptionDto.WebAuthnPair)} or {nameof(NotificationSubscriptionDto.JwtPair)} not null");

            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("RemoveUserWebPushSubscriptions", subscriptionsToRemove);
        }

        public async Task CheckIfUserExists(string username)
        {
            var hubConnection = await GetHubConnectionAsync();
            await hubConnection.SendAsync("CheckIfUserExist", username);
        }
    }
}