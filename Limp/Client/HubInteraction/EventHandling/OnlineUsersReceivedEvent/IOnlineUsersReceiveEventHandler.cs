using ClientServerCommon.Models;

namespace Limp.Client.HubInteraction.EventHandling.OnlineUsersReceivedEvent
{
    public interface IOnlineUsersReceiveEventHandler
    {
        Task CallSubscribers(List<UserConnections> parameter);
        Guid Subscribe(Func<List<UserConnections>, Task> callback);
        void Unsubscribe(Guid subscriptionId);
    }
}