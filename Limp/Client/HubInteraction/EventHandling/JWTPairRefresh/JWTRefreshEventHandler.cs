using ClientServerCommon.Models.Login;

namespace Limp.Client.HubInteraction.EventHandling.JWTPairRefresh
{
    public class JWTRefreshEventHandler : IEventHandler<AuthResult>, IJWTRefreshEventHandler
    {
        private Dictionary<Guid, Func<AuthResult, Task>> Subscriptions { get; set; } = new();
        public async Task CallSubscribers(AuthResult parameter)
        {
            foreach (var subscription in Subscriptions.Values)
            {
                await subscription(parameter);
            }
        }

        public Guid Subscribe(Func<AuthResult, Task> callback)
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
