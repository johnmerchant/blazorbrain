using BlazorBrain.Api.Types;
using BlazorBrain.Api.Web;
using BlazorBrain.Infrastructure.Chat;
using BlazorBrain.Infrastructure.Maps;
using HotChocolate.Execution.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets(typeof(BlazorBrain.Api.Web.ServiceCollectionExtensions).Assembly);
builder.Services.AddJwtAuthentication(builder.Configuration);


builder.Services
    .AddRedis()
    .AddCors()
    .AddChatService(builder.Configuration)
    .AddAzureMaps(builder.Configuration)
    .AddGraphQLServer()
    .AddAuthorization()
    .AddSorting()
    .AddFiltering()
    .AddSubscriptions()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddMutationConventions()
    .AddSubscriptionType<Subscription>()
    .AddSocketSessionInterceptor<AuthenticationSocketInterceptor>()
    .SetRequestOptions(svc => new RequestExecutorOptions
    {
        ExecutionTimeout = TimeSpan.FromMinutes(10)
    });

var app = builder.Build();

app.UseRouting();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();

app.UseCors(cors =>
{
    cors
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
});

app.MapGraphQL();

app.Run();