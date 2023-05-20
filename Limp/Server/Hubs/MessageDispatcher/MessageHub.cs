﻿using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Limp.Server.Hubs.MessageDispatcher.Helpers;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Limp.Server.Utilities.HttpMessaging;
using Limp.Server.Utilities.Kafka;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs.MessageDispatcher
{
    public class MessageHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IMessageBrokerService _messageBrokerService;
        private readonly IUserConnectedHandler<MessageHub> _userConnectedHandler;
        private readonly IOnlineUsersManager _onlineUsersManager;

        public MessageHub
        (IServerHttpClient serverHttpClient,
        IMessageBrokerService messageBrokerService,
        IUserConnectedHandler<MessageHub> userConnectedHandler,
        IOnlineUsersManager onlineUsersManager)
        {
            _serverHttpClient = serverHttpClient;
            _messageBrokerService = messageBrokerService;
            _userConnectedHandler = userConnectedHandler;
            _onlineUsersManager = onlineUsersManager;
        }

        public async override Task OnConnectedAsync()
        {
            _userConnectedHandler.OnConnect(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            _userConnectedHandler.OnDisconnect(Context.ConnectionId, RemoveUserFromGroup: Groups.RemoveFromGroupAsync);
            await base.OnDisconnectedAsync(exception);
        }

        private static bool IsClientConnectedToHub(string username)
        {
            lock (InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
            {
                return InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Key == username);
            }
        }

        public async Task SetUsername(string accessToken)
        {
            await _userConnectedHandler.OnUsernameResolved
            (Context.ConnectionId, accessToken,
            Groups.AddToGroupAsync,
            Clients.Caller.SendAsync,
            Clients.Caller.SendAsync);
            await PushOnlineUsersToClients();
        }

        public async Task PushOnlineUsersToClients()
        {
            List<UserConnection> userConnections = _onlineUsersManager.GetOnlineUsers();
            await Clients.All.SendAsync("ReceiveOnlineUsers", userConnections);
        }

        /// <summary>
        /// Checks if target user is connected to the same hub.
        /// If so: sends him a message.
        /// If not: sends message to message broker.
        /// </summary>
        /// <param name="message">A message that needs to be send</param>
        /// <exception cref="ApplicationException"></exception>
        public async Task Dispatch(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.TargetGroup))
                throw new ArgumentException("Invalid target group of a message.");

            switch (IsClientConnectedToHub(message.TargetGroup))
            {
                case true:
                    await MessageSender.SendAsync(message, Clients);
                    break;

                case false:
                    await MessageSender.AddAsUnprocessedAsync(message, Clients);
                    break;

                default:
                    throw new ApplicationException("Could not dispatch a message");
            }
        }

        /// <summary>
        /// Sends message to a message broker system
        /// </summary>
        /// <param name="message">Message to ship</param>
        public async Task Ship(Message message)
        {
            await _messageBrokerService.ProduceAsync(message);
        }

        public async Task MessageReceived(Guid messageId) => await MessageSender.OnMessageReceived(messageId, Clients);
        public async Task GetAnRSAPublic(string username)
        {
            string? pubKey = await _serverHttpClient.GetAnRSAPublicKey(username);
            await Clients.Caller.SendAsync("ReceivePublicKey", username, pubKey);
        }
    }
}