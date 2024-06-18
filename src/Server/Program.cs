using Ethachat.Server.BackendServiceProxy.AuthenticationInstance;
using Ethachat.Server.BackendServiceProxy.HLS;
using Ethachat.Server.Hubs;
using Ethachat.Server.Hubs.MessageDispatcher;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage.
    InMemoryStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Utilities.HttpMessaging;
using Ethachat.Server.Utilities.UsernameResolver;
using Ethachat.Server.WebPushNotifications;
using EthachatShared.Constants;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSignalR()
    .AddMessagePackProtocol();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = long.MaxValue; });

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
});

builder.Services.AddScoped<IUserConnectedHandler<UsersHub>, UConnectionHandler>();
builder.Services.AddScoped<IUserConnectedHandler<MessageHub>, MDConnectionHandler>();
builder.Services.AddTransient<IOnlineUsersManager, OnlineUsersManager>();
builder.Services.AddSingleton<IWebPushSender, FirebasePushSender>();
builder.Services.AddTransient<IUsernameResolverService, UsernameResolverService>();
builder.Services.AddSingleton<ILongTermStorageService<Message>, InMemoryLongTermStorage>();
builder.Services.AddSingleton<ILongTermStorageService<EncryptedDataTransfer>, InMemoryLongTermTransferStorage>();
builder.Services.AddTransient<IServerHttpClient, ServerHttpClient>();

var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapHub<AuthHub>(HubRelativeAddresses.AuthHubRelativeAddress);
app.MapHub<UsersHub>(HubRelativeAddresses.UsersHubRelativeAddress);
app.MapHub<MessageHub>(HubRelativeAddresses.MessageHubRelativeAddress);
app.MapFallbackToFile("index.html");

#if DEBUG
    app.UseHlsProxyService(builder.Configuration);
    app.UseAuthenticationInstanceProxyService(builder.Configuration);
#endif

app.Run();