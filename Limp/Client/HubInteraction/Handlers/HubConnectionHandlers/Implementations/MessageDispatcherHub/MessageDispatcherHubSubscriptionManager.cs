using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;

namespace Limp.Client.HubInteraction.Handlers.HubConnectionHandlers.Implementations.MessageDispatcherHub
{
    public class MessageDispatcherHubSubscriptionManager
    {
        public static List<Action<Message>> OnMessageReceived { get; set; } = new();
        public static List<Action<Guid>> OnMessageReceivedByRecepient { get; set; } = new();
        public static List<Action<string, string>> OnReceivePublicKey { get; set; } = new();
        public static List<Action<string>> OnMyNameResolved { get; set; } = new();
        public static List<Action<List<UserConnections>>> OnOnlineUsersReceived { get; set; } = new();
        public static void SubscribeToOnMessageReceived(Action<Message> callback)
        {
            OnMessageReceived.Add(callback);
        }
        public static void CallMessageReceived(Message message)
        {
            foreach (var method in OnMessageReceived)
            {
                method(message);
            }
        }
        public static void SubscribeToOnMessageReceivedByRecepient(Action<Guid> callback)
        {
            OnMessageReceivedByRecepient.Add(callback);
        }
        public static void CallMessageReceivedByRecepient(Guid messageId)
        {
            foreach (var method in OnMessageReceivedByRecepient)
            {
                method(messageId);
            }
        }
        public static void SubscribeToOnReceivePublicKey(Action<string, string> callback)
        {
            OnReceivePublicKey.Add(callback);
        }
        public static void CallReceivePublicKey(string partnersUsername, string partnersPublicKey)
        {
            foreach (var method in OnReceivePublicKey)
            {
                method(partnersUsername, partnersPublicKey);
            }
        }
        public static void SubscribeToOnMyNameResolved(Action<string> callback)
        {
            OnMyNameResolved.Add(callback);   
        }
        public static void CallMyNameResolved(string username)
        {
            foreach(var method in OnMyNameResolved)
            {
                method(username);
            }
        }
        public static void SubscribeToOnlineUsersReceived(Action<List<UserConnections>> callback)
        {
            OnOnlineUsersReceived.Add(callback);
        }
        public static void CallOnlineUsersReceived(List<UserConnections> userConnections)
        {
            foreach (var method in OnOnlineUsersReceived)
            {
                method(userConnections);
            }
        }
    }
}
