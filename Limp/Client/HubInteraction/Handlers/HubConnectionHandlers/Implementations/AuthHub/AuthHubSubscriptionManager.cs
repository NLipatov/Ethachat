using ClientServerCommon.Models.Login;

namespace Limp.Client.HubInteraction.Handlers.HubConnectionHandlers.Implementations.AuthHub
{
    public class AuthHubSubscriptionManager
    {
        private static List<Action<AuthResult>> OnJWTPairRefresh { get; set; } = new();
        public static void SubscribeToJWTPairRefresh(Action<AuthResult> callback)
        {
            OnJWTPairRefresh.Add(callback);
        }
        public static void CallJWTPairRefresh(AuthResult result)
        {
            foreach (var action in OnJWTPairRefresh)
            {
                action(result);
            }
        }
        public static void UnsubscriveAll()
        {
            OnJWTPairRefresh.Clear();
        }
    }
}
