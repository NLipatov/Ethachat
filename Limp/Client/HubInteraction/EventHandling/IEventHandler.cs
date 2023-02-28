namespace Limp.Client.HubInteraction.EventHandling
{
    public interface IEventHandler<T>
    {
        Guid Subscribe(Func<T, Task> callback);
        void Unsubscribe(Guid subscriptionId);
        Task CallSubscribers(T parameter);
    }
}
