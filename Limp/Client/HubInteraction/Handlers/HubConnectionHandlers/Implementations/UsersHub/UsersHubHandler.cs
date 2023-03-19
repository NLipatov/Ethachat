using ClientServerCommon.Models;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.Handlers.HubConnectionHandlers.Contract;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction.Handlers.HubConnectionHandlers.Implementations.UsersHub;

public class UsersHubHandler : IHubHandler<UsersHubHandler>
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jSRuntime;
    private readonly ICryptographyService _cryptographyService;
    private HubConnection? usersHub;
    public UsersHubHandler
    (NavigationManager navigationManager,
    IJSRuntime jSRuntime,
    ICryptographyService cryptographyService)
    {
        _navigationManager = navigationManager;
        _jSRuntime = jSRuntime;
        _cryptographyService = cryptographyService;
    }

    public async Task<HubConnection> ConnectAsync()
    {
        usersHub = new HubConnectionBuilder()
        .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
        .Build();

        usersHub.On<List<UserConnections>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
        {
            UsersHubSubscriptionManager.CallUsersConnectionsReceived(updatedTrackedUserConnections);
        });

        usersHub.On<string>("ReceiveConnectionId", connectionId =>
        {
            UsersHubSubscriptionManager.CallConnectionIdReceived(connectionId);
        });

        usersHub.On<string>("onNameResolve", async username =>
        {
            UsersHubSubscriptionManager.CallUsernameResolved(username);

            await usersHub.SendAsync("PostAnRSAPublic", username, InMemoryKeyStorage.MyRSAPublic.Value);
        });

        await usersHub.StartAsync();

        await usersHub.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

        return usersHub;
    }

    public async ValueTask DisposeAsync()
    {
        UsersHubSubscriptionManager.UnsubscriveAll();
        await DisposeHub();
    }

    private async Task DisposeHub()
    {
        if (usersHub == null)
            return;

        await usersHub.StopAsync();
        await usersHub.DisposeAsync();
    }
}
