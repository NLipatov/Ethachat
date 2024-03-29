using Ethachat.Server.Extensions;
using Ethachat.Server.Hubs;
using Ethachat.Server.Hubs.MessageDispatcher;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageSender;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.Handlers;
using Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling.OnlineUsersRequestEvent;
using Ethachat.Server.Services.LogService;
using Ethachat.Server.Services.LogService.Implementations.Seq;
using Ethachat.Server.Utilities.Redis;
using Ethachat.Server.Utilities.Redis.RedisConnectionConfigurer;
using Ethachat.Server.Utilities.Redis.UnsentMessageHandling;
using Ethachat.Server.Utilities.UsernameResolver;
using Ethachat.Server.WebPushNotifications;
using EthachatShared.Constants;
using Microsoft.AspNetCore.ResponseCompression;

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

builder.Services.UseServerHttpClient();

builder.Services.UseKafkaService();

builder.Services.AddScoped<IUserConnectedHandler<UsersHub>, UConnectionHandler>();
builder.Services.AddScoped<IUserConnectedHandler<MessageHub>, MDConnectionHandler>();
builder.Services.AddTransient<IOnlineUsersManager, OnlineUsersManager>();
builder.Services.AddTransient<IMessageSendHandler, MessageSendHandler>();
builder.Services.AddTransient<IWebPushSender, FirebasePushSender>();
builder.Services.AddTransient<IUnsentMessagesRedisService, UnsentMessagesRedisService>();
builder.Services.AddTransient<IUsernameResolverService, UsernameResolverService>();
builder.Services.AddTransient<ILogService, SeqLogService>();
builder.Services.AddTransient<IRedisConnectionConfigurer, RedisConnectionConfigurer>();

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

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapHub<AuthHub>(HubRelativeAddresses.AuthHubRelativeAddress);
app.MapHub<UsersHub>(HubRelativeAddresses.UsersHubRelativeAddress);
app.MapHub<MessageHub>(HubRelativeAddresses.MessageHubRelativeAddress);
app.MapHub<LoggingHub>(HubRelativeAddresses.ExceptionLoggingHubRelativeAddress);
app.MapFallbackToFile("index.html");

app.Run();
