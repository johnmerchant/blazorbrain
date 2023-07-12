namespace BlazorBrain.Infrastructure.Maps;


public class AzureMapsOptions
{
    public string ClientId { get; set; } = default!;
    public string SubscriptionKey { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string AppId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
}