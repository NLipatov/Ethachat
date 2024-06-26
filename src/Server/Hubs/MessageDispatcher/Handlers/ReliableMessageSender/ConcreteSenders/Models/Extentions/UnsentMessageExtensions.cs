using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models.Extentions;

public static class UnsentMessageExtensions
{
    public static UnsentItem ToUnsentMessage(this Message message)
    {
        return new()
        {
            Message = message
        };
    }
}