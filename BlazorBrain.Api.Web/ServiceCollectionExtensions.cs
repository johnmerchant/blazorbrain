using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using StackExchange.Redis;

namespace BlazorBrain.Api.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer("Websockets", ctx => { })
            .AddMicrosoftIdentityWebApi(options =>
            {
                configuration.GetRequiredSection("AzureAd").Bind(options);
                options.ForwardDefaultSelector = context =>
                {
                    if (!context.Items.ContainsKey(AuthenticationSocketInterceptor.HTTP_CONTEXT_WEBSOCKET_AUTH_KEY) && context.Request.Headers.TryGetValue("Upgrade", out var value) && value.Count > 0 && value[0] == "websocket")
                    {
                        return "Websockets";
                    }
                    return JwtBearerDefaults.AuthenticationScheme;
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Items.TryGetValue(AuthenticationSocketInterceptor.HTTP_CONTEXT_WEBSOCKET_AUTH_KEY, out var token) && token is not null)
                        {
                            context.Token = token.ToString();
                        }
                        return Task.CompletedTask;
                    }
                };
            }, microsoftIdentityOptions =>
            {
                configuration.GetRequiredSection("AzureAd").Bind(microsoftIdentityOptions);
            });
        
        return serviceCollection;
    }

    public static IServiceCollection AddRedis(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IConnectionMultiplexer>(svc =>
        {
            var connectionString = svc.GetRequiredService<IConfiguration>().GetConnectionString("Redis")!;
            return ConnectionMultiplexer.Connect(connectionString);
        });
        return serviceCollection;
    }
    
}