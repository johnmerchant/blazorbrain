using Newtonsoft.Json;

namespace BlazorBrain.Application.Abstractions.Chat;

public record Conversation(
    [property: JsonProperty("id")]
    Guid Id,
    string UserId,
    string Title,
    IReadOnlyList<ConversationMessage> Messages,
    bool IsStreaming,
    DateTimeOffset Created
);