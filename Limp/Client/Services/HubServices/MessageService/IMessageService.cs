﻿using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.MessageService
{
    public interface IMessageService
    {
        Task<HubConnection> ConnectAsync();
        Task DisconnectAsync();
        Task ReconnectAsync();
        bool IsConnected();
        Task SendMessage(Message message);
        Task RequestForPartnerPublicKey(string partnerUsername);
        Guid SubscribeToPartnerAESAccept(Func<string, Task> callback);
        void RemoveSubscriptionToPartnerAESAccept(Guid subscriptionId);
        Guid SubscribeToMessageReceivedByRecepient(Action<Guid> callback);
        void RemoveSubscriptionToMessageReceivedByRecepient(Guid subscriptionId);
    }
}
