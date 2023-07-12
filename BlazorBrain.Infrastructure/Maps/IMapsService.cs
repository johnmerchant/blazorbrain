namespace BlazorBrain.Infrastructure.Maps;

public interface IMapsService
{
    Task<string> GetAccessToken(CancellationToken cancellationToken);
}