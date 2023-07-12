using Newtonsoft.Json;

namespace BlazorBrain.Application.Abstractions.Chat;

public record ConversationListing(
    [JsonProperty("id")]
    Guid Id,
    string UserId,
    string Title,
    DateTimeOffset Created
);