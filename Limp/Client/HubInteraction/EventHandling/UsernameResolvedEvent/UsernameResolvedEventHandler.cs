using ClientServerCommon.Models;

namespace Limp.Client.HubInteraction.EventHandling.UsernameResolvedEvent
{
    public class UsernameResolvedEventHandler : IEventHandler<string>
    {
        private Dictionary<Guid, Func<string, Task>> Subscriptions { get; set; } = new();
        public async Task CallSubscribers(string parameter)
        {
            foreach (var subscription in Subscriptions.Values)
            {
                await subscription(parameter);
            }
        }

        public Guid Subscribe(Func<string, Task> callback)
        {
            Guid guid = Guid.NewGuid();
            Subscriptions.Add(guid, callback);
            return guid;
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            Subscriptions.Remove(subscriptionId);
        }
    }
}
