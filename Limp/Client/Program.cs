using BlazorBootstrap;
using Limp.Client;
using Limp.Client.Cryptography;
using Limp.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.Handlers.MessageDecryption;
using Limp.Client.Pages.AccountManagement.LogicHandlers;
using Limp.Client.Pages.Chat.Logic.MessageBuilder;
using Limp.Client.Services.CloudKeyService;
using Limp.Client.Services.ConcurrentCollectionManager;
using Limp.Client.Services.ConcurrentCollectionManager.Implementations;
using Limp.Client.Services.ContactsProvider;
using Limp.Client.Services.ContactsProvider.Implementations;
using Limp.Client.Services.HubConnectionProvider;
using Limp.Client.Services.HubConnectionProvider.Implementation;
using Limp.Client.Services.HubService.AuthService;
using Limp.Client.Services.HubService.AuthService.Implementation;
using Limp.Client.Services.HubService.UsersService;
using Limp.Client.Services.HubService.UsersService.Implementation;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor.Implementation;
using Limp.Client.Services.HubServices.CommonServices.SubscriptionService;
using Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Implementation;
using Limp.Client.Services.HubServices.MessageService;
using Limp.Client.Services.HubServices.MessageService.Implementation;
using Limp.Client.Services.HubServices.UndeliveredMessageSending;
using Limp.Client.Services.InboxService;
using Limp.Client.Services.InboxService.Implementation;
using Limp.Client.Services.LocalKeyChainService.Implementation;
using Limp.Client.Services.NotificationService;
using Limp.Client.Services.NotificationService.Implementation;
using Limp.Client.Services.UndeliveredMessagesStore;
using Limp.Client.Services.UndeliveredMessagesStore.Implementation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IMessageBox, MessageBox>();
builder.Services.AddTransient<IMessageDecryptor, MessageDecryptor>();
builder.Services.AddTransient<IAESOfferHandler, AESOfferHandler>();
builder.Services.AddTransient<IEventCallbackExecutor, EventCallbackExecutor>();
builder.Services.AddTransient<IContactsProvider, ContactsProvider>();
builder.Services.AddTransient<IConcurrentCollectionManager, ConcurrentCollectionManager>();
builder.Services.AddScoped<IHubConnectionProvider, HubConnectionProvider>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IUsersService, UsersService>();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<IHubServiceSubscriptionManager, HubServiceSubscriptionManager>();
builder.Services.AddTransient<ICallbackExecutor, CallbackExecutor>();
builder.Services.AddTransient<IMessageBuilder, MessageBuilder>();
builder.Services.AddTransient<IBrowserKeyStorage, BrowserKeyStorage>();
builder.Services.AddTransient<IUndeliveredMessagesRepository, UndeliveredMessagesRepository>();
builder.Services.AddTransient<IUndeliveredMessageService, UndeliveredMessageService>();
builder.Services.AddTransient<ILoginHandler, LoginHandler>();
builder.Services.AddTransient<IWebPushService, WebPushService>();

await builder.Build().RunAsync();
