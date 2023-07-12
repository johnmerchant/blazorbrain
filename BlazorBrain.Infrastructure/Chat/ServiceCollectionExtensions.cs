using Azure;
using Azure.AI.OpenAI;
using BlazorBrain.Application.Abstractions.Chat;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorBrain.Infrastructure.Chat;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatService(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton<CosmosClient>(svc =>
        {
            var connectionString = svc.GetRequiredService<IConfiguration>().GetConnectionString("Cosmos");
            return new CosmosClient(connectionString);
        });
        
        serviceCollection.AddOpenAIClient(configuration);
        serviceCollection.AddTransient<IChatService, ChatService>();
        return serviceCollection;
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<OpenAIOptions>(configuration.GetSection("OpenAI"));
        serviceCollection.AddTransient(svc =>
        {
            var options = svc.GetRequiredService<IOptions<OpenAIOptions>>();
            return new OpenAIClient(new Uri(options.Value.Endpoint), new AzureKeyCredential(options.Value.Key));
        });
        return serviceCollection;
    }
    
}