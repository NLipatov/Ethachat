﻿using ClientServerCommon.Models;
using Limp.Client.Cryptography;
using Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.EventTypes;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub.EventTypes;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Limp.Client.Services.HubConnectionProvider.Implementation.HubInteraction.Implementations;
using Limp.Client.TopicStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.Services.HubConnectionProvider.Implementation
{
    public class HubConnectionProvider : IAsyncDisposable, IHubConnectionProvider
    {
        public HubConnectionProvider
        (IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        IHubObserver<AuthHubEvent> authHubObserver,
        IHubObserver<UsersHubEvent> usersHubObserver,
        IHubObserver<MessageHubEvent> messageDispatcherHubObserver,
        ICryptographyService cryptographyService,
        IAESOfferHandler aESOfferHandler,
        IMessageBox messageBox)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _authHubObserver = authHubObserver;
            _usersHubObserver = usersHubObserver;
            _messageDispatcherHubObserver = messageDispatcherHubObserver;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _messageBox = messageBox;
        }
        private AuthHubInteractor? _authHubInteractor;
        private UsersHubInteractor? _usersHubInteractor;
        private MessageDispatcherHubInteractor? _messageDispatcherHubInteractor;
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly IHubObserver<AuthHubEvent> _authHubObserver;
        private readonly IHubObserver<UsersHubEvent> _usersHubObserver;
        private readonly IHubObserver<MessageHubEvent> _messageDispatcherHubObserver;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAESOfferHandler _aESOfferHandler;
        private readonly IMessageBox _messageBox;
        private List<Guid> usersHubHandlers = new();
        private List<Guid> authHubHandlers = new();
        private List<Guid> messageDispatcherHandlers = new();
        private HubConnection authHubConnection;
        private HubConnection? usersHubConnection;

        public async Task ConnectToHubs
        (Func<List<UserConnection>, Task>? OnUserConnectionsUpdate,
        Func<string, Task>? OnConnectionId = null,
        Action? RerenderComponent = null)
        {
            //If user does not have at least one token from JWT pair, ask him to login
            string? accessToken = await JWTHelper.GetAccessToken(_jSRuntime);
            if (string.IsNullOrWhiteSpace(accessToken)
                ||
                string.IsNullOrWhiteSpace(await JWTHelper.GetRefreshToken(_jSRuntime)))
            {
                _navigationManager.NavigateTo("login");
                return;
            }

            _authHubInteractor = new AuthHubInteractor(_navigationManager, _jSRuntime, _authHubObserver);
            _usersHubInteractor = new UsersHubInteractor(_navigationManager, _jSRuntime, _usersHubObserver);

            usersHubHandlers.Add(_usersHubObserver.AddHandler<Func<string, Task>>(UsersHubEvent.ConnectionIdReceived,
            async (id) =>
            {
                await InvokeCallbackIfExists(OnConnectionId, id);
            }));

            usersHubHandlers.Add(_usersHubObserver.AddHandler<Func<List<UserConnection>, Task>>(UsersHubEvent.ConnectedUsersListReceived,
            async (userConnectionsList) =>
            {
                await InvokeCallbackIfExists(OnUserConnectionsUpdate, userConnectionsList);
                if (RerenderComponent != null)
                    RerenderComponent();
            }));

            usersHubHandlers.Add(_usersHubObserver.AddHandler<Func<string, Task>>(UsersHubEvent.MyUsernameResolved,
            async (username) =>
            {
                await _messageDispatcherHubInteractor!.ConnectAsync();
                messageDispatcherHandlers.Add(_messageDispatcherHubObserver
                    .AddHandler(MessageHubEvent.OnlineUsersReceived, OnUserConnectionsUpdate));

                if (RerenderComponent != null)
                    RerenderComponent();
            }));

            authHubConnection = await _authHubInteractor.ConnectAsync(); //Connecting to authHub
            usersHubConnection = await _usersHubInteractor.ConnectAsync(); //Connection to usersHub

            //Creating interactor, but not connecting to hub
            //We will connect to this hub only when client's username is resolved
            _messageDispatcherHubInteractor = new MessageDispatcherHubInteractor
                (_navigationManager,
                _jSRuntime,
                _messageDispatcherHubObserver,
                _cryptographyService,
                _aESOfferHandler,
                _messageBox,
                usersHubConnection,
                authHubConnection);
        }

        private async Task InvokeCallbackIfExists<T>(Func<T, Task>? callback, T parameter)
        {
            if (callback != null)
                await callback.Invoke(parameter);
        }

        public async ValueTask DisposeAsync()
        {
            if (_authHubInteractor != null)
            {
                await _authHubInteractor.DisposeAsync();
            }
            if (_usersHubInteractor != null)
            {
                await _usersHubInteractor.DisposeAsync();
            }
            if (_messageDispatcherHubInteractor != null)
            {
                await _messageDispatcherHubInteractor.DisposeAsync();
            }
        }
    }
}
