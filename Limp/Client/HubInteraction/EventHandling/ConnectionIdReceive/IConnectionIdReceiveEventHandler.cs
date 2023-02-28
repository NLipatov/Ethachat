namespace Limp.Client.HubInteraction.EventHandling.ConnectionIdReceive
{
    public interface IConnectionIdReceiveEventHandler
    {
        Task CallSubscribers(string parameter);
        Guid Subscribe(Func<string, Task> callback);
        void Unsubscribe(Guid subscriptionId);
    }
}