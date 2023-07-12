using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace BlazorBrain.Infrastructure.Maps;

public class MapsService : IMapsService
{
    private const string AuthorityFormat = "https://login.microsoftonline.com/{0}/oauth2/v2.0";
    private const string Scope = "https://atlas.microsoft.com/.default";

    private readonly AzureMapsOptions _options;
    
    public MapsService(IOptions<AzureMapsOptions> options)
    {
        _options = options.Value;
    }
    
    public async Task<string> GetAccessToken(CancellationToken cancellationToken)
    {
        var clientApp = ConfidentialClientApplicationBuilder.Create(_options.AppId)
            .WithAuthority(string.Format(AuthorityFormat, _options.TenantId))
            .WithClientSecret(_options.ClientSecret)
            .Build();
        
        var result = await clientApp.AcquireTokenForClient(new[] { Scope }).ExecuteAsync(cancellationToken);
        
        return result.AccessToken;
    }
}