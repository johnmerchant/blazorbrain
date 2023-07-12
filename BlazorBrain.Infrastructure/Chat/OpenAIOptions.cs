namespace BlazorBrain.Infrastructure.Chat;

public class OpenAIOptions
{
    public string DeploymentName { get; set; } = default!;
    public string Endpoint { get; set; } = default!;
    public string Key { get; set; } = default!;
}