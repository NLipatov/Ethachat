using Limp.Server.Hubs.Models;
using System.Collections.Concurrent;

namespace Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage
{
    public static class InMemoryHubConnectionStorage
    {
        public static List<UserHubUser> ConnectedUsers = new();
        public static ConcurrentDictionary<string, List<string>> UsersHubConnections { get; set; } = new();
        public static ConcurrentDictionary<string, List<string>> MessageDispatcherHubConnections { get; set; } = new();
    }
}
