﻿using ClientServerCommon.Models;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent
{
    public class OnlineUsersManager : IOnlineUsersManager
    {
        public List<UserConnection> GetOnlineUsers()
        {
            List<UserConnection> mdConnections = InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Username != null && x.ConnectionIds.Count > 0)
                .ToList();

            List<UserConnection> uConnections = InMemoryHubConnectionStorage.UsersHubConnections
                .Where(x => x.Username != null && x.ConnectionIds.Count > 0)
                .ToList();

            List<UserConnection> commonConnections = uConnections.Where(u=>mdConnections.Any(md=>md.Username == u.Username)).ToList();

            return commonConnections;
        }
    }
}