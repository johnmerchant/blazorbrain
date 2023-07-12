using BlazorBrain.Application.Abstractions.Chat;

namespace BlazorBrain.Api.Types.Chat;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Content { get; set; } = "";
    public ConversationRole Role { get; set; }
    public string Description { get; set; } = "";
    public static ConversationMessage FromDomain(Application.Abstractions.Chat.ConversationMessage message) => new()
    {
        Id = message.Id,
        UserId = message.UserId,
        Content = string.Join("", message.Content),
        Role = message.Role,
        Description = message.Description ?? ""
    };
}