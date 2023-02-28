using ClientServerCommon.Models.Login;

namespace Limp.Client.HubInteraction.EventHandling.JWTPairRefresh
{
    public interface IJWTRefreshEventHandler
    {
        Task CallSubscribers(AuthResult parameter);
        Guid Subscribe(Func<AuthResult, Task> callback);
        void Unsubscribe(Guid subscriptionId);
    }
}