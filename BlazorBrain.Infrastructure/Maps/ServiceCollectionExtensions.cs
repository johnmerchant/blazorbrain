using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorBrain.Infrastructure.Maps;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureMaps(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<AzureMapsOptions>(configuration.GetSection("AzureMaps"));
        serviceCollection.AddTransient<IMapsService, MapsService>();
        return serviceCollection;
    }
    
}