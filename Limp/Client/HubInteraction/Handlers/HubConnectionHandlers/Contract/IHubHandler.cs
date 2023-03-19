using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubInteraction.Handlers.HubConnectionHandlers.Contract
{
    public interface IHubHandler<T> : IAsyncDisposable
    {
        /// <summary>
        /// Connects to hub, register event-callbacks
        /// </summary>
        Task<HubConnection> ConnectAsync();
    }
}
