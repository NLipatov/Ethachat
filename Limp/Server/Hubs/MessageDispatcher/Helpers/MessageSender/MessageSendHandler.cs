﻿using ClientServerCommon.Models.Message;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender
{
    public class MessageSendHandler : IMessageSendHandler
    {
        public async Task SendAsync(Message message, IHubCallerClients clients)
        {
            if (string.IsNullOrWhiteSpace(message.TargetGroup))
                return;

            //For personal chat we have Group with only one person in it
            //Send to members of this single-membered Group a message
            await clients.Group(message.TargetGroup).SendAsync("ReceiveMessage", message.ToReceiverRepresentation());

            await clients.Caller.SendAsync("ReceiveMessage", message.ToSenderRepresentation());
            //In the other case we need some message storage to be implemented to store a not delivered messages and remove them when they are delivered.
        }

        public async Task MarkAsReceived(Guid messageId, string topicName, IHubCallerClients clients)
        {
            if (string.IsNullOrWhiteSpace(topicName))
                throw new ApplicationException("Cannot get an message sender username.");

            await clients.Group(topicName).SendAsync("MessageWasReceivedByRecepient", messageId);
        }
    }
}