namespace BlazorBrain.Application.Abstractions.Chat;

public interface IChatService
{
    IAsyncEnumerable<ConversationListing> List(string userId, CancellationToken cancellationToken);
    Task<Conversation> Load(string userId, Guid conversationId, CancellationToken cancellationToken);
    Task<Conversation> Create(string userId, CancellationToken cancellationToken);
    
    IAsyncEnumerable<Conversation> StreamPrompt(string userId, Guid conversationId, string message, CancellationToken cancellationToken);
    IAsyncEnumerable<Conversation> StreamResponse(string userId, Guid conversationId, string userMessage, CancellationToken cancellationToken);
    IAsyncEnumerable<ConversationMessage> LoadMessages(string userId, Guid id, CancellationToken cancellationToken);
}