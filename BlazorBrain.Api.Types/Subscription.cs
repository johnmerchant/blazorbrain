using System.Security.Claims;
using BlazorBrain.Application.Abstractions.Chat;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace BlazorBrain.Api.Types;

public class Subscription
{
    public async ValueTask<ISourceStream<Conversation>> SubscribeToConversationCreated(
        ClaimsPrincipal claimsPrincipal,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var topic = $"{userId}_ConversationCreated";
        return await receiver.SubscribeAsync<Conversation>(topic, cancellationToken);
    }

    [Subscribe(With = nameof(SubscribeToConversationCreated))]
    public Chat.Conversation OnConversationCreated([EventMessage] Conversation conversation) => Chat.Conversation.FromDomain(conversation);

    public async ValueTask<ISourceStream<Conversation>> SubscribeToConversationUpdated(
        ClaimsPrincipal claimsPrincipal,
        [Service] ITopicEventReceiver receiver,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var topic = $"{userId}_{conversationId}_ConversationUpdated";
        return await receiver.SubscribeAsync<Conversation>(topic, cancellationToken);
    }

    [Subscribe(With = nameof(SubscribeToConversationUpdated))]
    public Chat.Conversation OnConversationUpdated([EventMessage] Conversation conversation) => Chat.Conversation.FromDomain(conversation);
}