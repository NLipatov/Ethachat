using ClientServerCommon.Models;
using ClientServerCommon.Models.Login;
using ClientServerCommon.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubInteraction
{
    public interface IHubInteractor
    {
        Task<HubConnection> ConnectToAuthHubAsync(string accessToken, string refreshToken);
        Task<HubConnection> ConnectToMessageDispatcherHubAsync(string accessToken, Action<Message>? onMessageReceive = null, Action<string>? onUsernameResolve = null, Action<Guid>? onMessageReceivedByRecepient = null);
        Task<HubConnection> ConnectToUsersHubAsync(string accessToken);
        Task DisposeAsync();
        bool IsMessageHubConnected();
        Task SendMessage(Message message);
        Task UpdateRSAPublicKeyAsync(string username);
    }
}