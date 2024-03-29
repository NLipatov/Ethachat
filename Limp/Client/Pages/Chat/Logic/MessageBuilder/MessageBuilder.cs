﻿using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Pages.Chat.Logic.MessageBuilder
{
    public class MessageBuilder : IMessageBuilder
    {
        private readonly ICryptographyService _cryptographyService;

        public MessageBuilder(ICryptographyService cryptographyService)
        {
            _cryptographyService = cryptographyService;
        }
        public async Task<Message> BuildMessageToBeSend(string plainMessageText, string topicName, string myName, Guid id, MessageType type)
        {
            Cryptogramm cryptogramm = await _cryptographyService
                .EncryptAsync<AESHandler>(new Cryptogramm
                {
                    Cyphertext = plainMessageText,
                }, contact: topicName);

            Message messageToSend = new Message
            {
                Type = type,
                Id = id,
                Cryptogramm = cryptogramm,
                DateSent = DateTime.UtcNow,
                TargetGroup = topicName!,
                Sender = myName ?? throw new ApplicationException
                    ($"Exception on message building phase: Cannot define message sender name."),
            };

            return messageToSend;
        }
    }
}
