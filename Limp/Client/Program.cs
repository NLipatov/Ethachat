using Limp.Client;
using Limp.Client.Cryptography;
using Limp.Client.HubInteraction;
using Limp.Client.HubInteraction.EventHandling.OnlineUsersReceivedEvent;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IHubInteractor, HubInteractor>();
builder.Services.AddSingleton<IOnlineUsersReceiveEventHandler, OnlineUsersReceiveEventHandler>();

await builder.Build().RunAsync();
