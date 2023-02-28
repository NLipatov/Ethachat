using ClientServerCommon.Models;

namespace Limp.Client.HubInteraction.EventHandling.OnlineUsersReceivedEvent
{
    public class OnlineUsersReceiveEventHandler : IEventHandler<List<UserConnections>>, IOnlineUsersReceiveEventHandler
    {
        private Dictionary<Guid, Func<List<UserConnections>, Task>> Subscriptions { get; set; } = new();
        public Guid Subscribe(Func<List<UserConnections>, Task> callback)
        {
            Guid guid = Guid.NewGuid();
            Subscriptions.Add(guid, callback);
            return guid;
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            Subscriptions.Remove(subscriptionId);
        }

        public async Task CallSubscribers(List<UserConnections> parameter)
        {
            foreach (var subscription in Subscriptions.Values)
            {
                await subscription(parameter);
            }
        }
    }
}
