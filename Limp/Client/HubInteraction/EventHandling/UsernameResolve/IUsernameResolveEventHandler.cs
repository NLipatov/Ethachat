namespace Limp.Client.HubInteraction.EventHandling.UsernameResolve
{
    public interface IUsernameResolveEventHandler
    {
        Task CallSubscribers(string parameter);
        Guid Subscribe(Func<string, Task> callback);
        void Unsubscribe(Guid subscriptionId);
    }
}