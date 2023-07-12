using System.Security.Claims;
using BlazorBrain.Application.Abstractions.Chat;
using BlazorBrain.Infrastructure.Maps;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;

namespace BlazorBrain.Api.Types;

public class Mutation
{
    [Authorize]
    public async Task<Chat.Conversation> CreateConversation(
        ClaimsPrincipal claimsPrincipal, 
        [Service] IChatService chatService, 
        [Service] ITopicEventSender sender,
        CancellationToken cancellationToken
    )
    {
        var userId = claimsPrincipal.GetUserId();
        var conversation = await chatService.Create(userId, cancellationToken);
        var result = Chat.Conversation.FromDomain(conversation);
        await sender.SendAsync($"{userId}_ConversationCreated", conversation, cancellationToken);
        return result;
    }

    [Authorize]
    public async Task<Chat.Conversation> Prompt(
        ClaimsPrincipal claimsPrincipal,
        [Service] IChatService chatService,
        [Service] ITopicEventSender sender,
        Guid conversationId,
        string message,
        CancellationToken cancellationToken
    )
    {
        var userId = claimsPrincipal.GetUserId();
        var stream = chatService.StreamPrompt(userId, conversationId, message, cancellationToken);
        Chat.Conversation? result = null;
        var topic = $"{userId}_{conversationId}_ConversationUpdated";
        var tasks = new List<Task>();
        await foreach (var conversation in stream.WithCancellation(cancellationToken))
        {
            result = Chat.Conversation.FromDomain(conversation);
            tasks.Add(sender.SendAsync(topic, conversation, cancellationToken).AsTask());
        }
        await Task.WhenAll(tasks);
        if (result is null) throw new InvalidOperationException();
        return result;
    }
    
    [Authorize]
    public async Task<Chat.Conversation> AddMessage(
        ClaimsPrincipal claimsPrincipal, 
        [Service] IChatService chatService, 
        [Service] ITopicEventSender sender,
        Guid conversationId, 
        string message, 
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var stream = chatService.StreamResponse(userId, conversationId, message, cancellationToken);
        Chat.Conversation? result = null;
        var topic = $"{userId}_{conversationId}_ConversationUpdated";
        var tasks = new List<Task>();
        await foreach (var conversation in stream.WithCancellation(cancellationToken))
        {
            result = Chat.Conversation.FromDomain(conversation);
            tasks.Add(sender.SendAsync(topic, conversation, cancellationToken).AsTask());
        }

        await Task.WhenAll(tasks);

        if (result is null) throw new InvalidOperationException();
        return result;
    }

    
   [Authorize]
   public async Task<string> MapsAccessToken([Service] IMapsService mapsService, CancellationToken cancellationToken)
   {
      return await mapsService.GetAccessToken(cancellationToken);
   }
   
}