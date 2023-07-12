using System.Runtime.CompilerServices;
using BlazorBrain.Application.Abstractions.Chat;
using HotChocolate;

namespace BlazorBrain.Api.Types.Chat;

public class Conversation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public bool IsStreaming { get; set; }
    
    [GraphQLIgnore]
    public IReadOnlyList<ConversationMessage>? Messages { private get; init; }

    public DateTimeOffset Created { get; set; }

    public async IAsyncEnumerable<ConversationMessage> GetMessages([Service] IChatService chatService, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Messages is not null)
        {
            foreach (var message in Messages)
            {
                yield return message;
            }
            yield break;
        }
        
        var messages = chatService.LoadMessages(UserId, Id, cancellationToken);
        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            yield return ConversationMessage.FromDomain(message);
        }
    }

    public static Conversation FromDomain(Application.Abstractions.Chat.Conversation conversation) => new()
    {
        Id = conversation.Id,
        UserId = conversation.UserId,
        Title = conversation.Title,
        Messages = conversation.Messages.Select(ConversationMessage.FromDomain).ToArray(),
        IsStreaming = conversation.IsStreaming,
        Created = conversation.Created
    };
}