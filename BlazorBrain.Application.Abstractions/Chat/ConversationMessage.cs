namespace BlazorBrain.Application.Abstractions.Chat;

public record ConversationMessage(
    Guid Id, 
    string UserId,
    ConversationRole Role, 
    IReadOnlyList<string> Content, 
    string? Description = null
);