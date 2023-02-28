using Limp.Client;
using Limp.Client.Cryptography;
using Limp.Client.HubInteraction;
using Limp.Client.HubInteraction.EventHandling.ConnectionIdReceive;
using Limp.Client.HubInteraction.EventHandling.OnlineUsersReceivedEvent;
using Limp.Client.HubInteraction.EventHandling.UsernameResolve;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IHubInteractor, HubInteractor>();
builder.Services.AddSingleton<IOnlineUsersReceiveEventHandler, OnlineUsersReceiveEventHandler>();
builder.Services.AddSingleton<IConnectionIdReceiveEventHandler, ConnectionIdReceiveEventHandler>();
builder.Services.AddSingleton<IUsernameResolveEventHandler, UsernameResolveEventHandler>();

await builder.Build().RunAsync();
