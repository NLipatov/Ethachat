using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.Handlers.HubConnectionHandlers.Contract;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction.Handlers.HubConnectionHandlers.Implementations.MessageDispatcherHub
{
    public class MessageDispatcherHandler : IHubHandler<MessageDispatcherHandler>
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jSRuntime;
        private HubConnection? messageDispatcherHub;
        public MessageDispatcherHandler(NavigationManager navigationManager,
            IJSRuntime jSRuntime)
        {
            _navigationManager = navigationManager;
            _jSRuntime = jSRuntime;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            messageDispatcherHub = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/messageDispatcherHub"))
                .Build();

            messageDispatcherHub.On<Message>("ReceiveMessage", message =>
            {
                MessageDispatcherHubSubscriptionManager.CallMessageReceived(message);
            });

            messageDispatcherHub.On<Guid>("MessageWasReceivedByRecepient", messageId =>
            {
                MessageDispatcherHubSubscriptionManager.CallMessageReceivedByRecepient(messageId);
            });

            messageDispatcherHub.On<string, string>("ReceivePublicKey", (partnersUsername, partnersPublicKey) =>
            {
                MessageDispatcherHubSubscriptionManager.CallReceivePublicKey(partnersUsername, partnersPublicKey);
            });

            messageDispatcherHub.On<string>("OnMyNameResolve", username =>
            {
                MessageDispatcherHubSubscriptionManager.CallMyNameResolved(username);
            });

            messageDispatcherHub.On<List<UserConnections>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                MessageDispatcherHubSubscriptionManager.CallOnlineUsersReceived(updatedTrackedUserConnections);
            });

            await messageDispatcherHub.StartAsync();

            await messageDispatcherHub.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

            return messageDispatcherHub;
        }

        public async ValueTask DisposeAsync()
        {
            if (messageDispatcherHub == null)
                return;

            await messageDispatcherHub.StopAsync();
            await messageDispatcherHub.DisposeAsync();
        }
    }
}
