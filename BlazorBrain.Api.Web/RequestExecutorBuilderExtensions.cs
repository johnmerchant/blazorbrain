using HotChocolate.Execution.Configuration;
using StackExchange.Redis;

namespace BlazorBrain.Api.Web;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddSubscriptions(this IRequestExecutorBuilder builder)
    {
        return builder.AddRedisSubscriptions(svc => svc.GetRequiredService<IConnectionMultiplexer>());
    }
}