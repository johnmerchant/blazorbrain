using AzureMapsControl.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorBrain.Client;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using StrawberryShake;
using StrawberryShake.Transport.WebSockets;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<CustomAuthorizationConnectionInterceptor>();
builder.Services.AddScoped<CustomAuthorizationMessageHandler>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("api://12f26c78-03e9-4ec7-8473-a9512beb9bcc/access_as_user");
});

builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services
    .AddWebSocketClient("");

builder.Services
    .AddBlazorBrainClient(ExecutionStrategy.CacheAndNetwork)
    .ConfigureHttpClient(
        client => client.BaseAddress = new Uri("http://localhost:5150/graphql"),
        clientBuilder => clientBuilder.AddHttpMessageHandler<CustomAuthorizationMessageHandler>())
    .ConfigureWebSocketClient(
        client => client.Uri = new Uri("ws://localhost:5150/graphql"),
        clientBuilder => clientBuilder.ConfigureConnectionInterceptor<CustomAuthorizationConnectionInterceptor>());

builder.Services.AddAzureMapsControl(config =>
{
    config.ClientId = "b066d5cb-a425-430a-96f5-8b25f44629df";
    config.AadTenant = "6b7d3a1d-4be6-417f-929f-37b0119ba799";
});

builder.Services
    .AddBlazorise(options => { options.Immediate = true; })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons();

await builder.Build().RunAsync();

public class CustomAuthorizationConnectionInterceptor : ISocketConnectionInterceptor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CustomAuthorizationConnectionInterceptor(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async ValueTask<object?> CreateConnectionInitPayload(ISocketProtocol protocol, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomAuthorizationConnectionInterceptor>>();
        try
        {
            var accessTokenProvider = scope.ServiceProvider.GetRequiredService<IAccessTokenProvider>();
            var accessToken = await accessTokenProvider.RequestAccessToken();
            accessToken.TryGetToken(out var token);
            if (!string.IsNullOrWhiteSpace(token?.Value))
            {
                return new Dictionary<string, object> { { "authToken", token.Value } };
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get token");
        }
        
        return new Dictionary<string, object>(0);
    }
}