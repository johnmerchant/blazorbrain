using System.Security.Claims;
using BlazorBrain.Application.Abstractions.Chat;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace BlazorBrain.Api.Types;

public class Query
{
   [Authorize]
   public string UserId(ClaimsPrincipal claimsPrincipal) => claimsPrincipal.GetUserId();

   [Authorize]
   public async Task<Chat.Conversation> Conversation(ClaimsPrincipal claimsPrincipal, Guid conversationId, [Service] IChatService chatService, CancellationToken cancellationToken)
   {
      var userId = claimsPrincipal.GetUserId();
      var conversation = await chatService.Load(userId, conversationId, cancellationToken);
      return Chat.Conversation.FromDomain(conversation);
   }
   
   [Authorize]
   [UsePaging]
   [UseFiltering]
   [UseSorting]
   public IEnumerable<Chat.Conversation> Conversations(ClaimsPrincipal claimsPrincipal, [Service] IChatService chatService, CancellationToken cancellationToken)
   {
      var userId = claimsPrincipal.GetUserId();
      var conversations = chatService.List(userId, cancellationToken);
      return conversations.Select(z => new Chat.Conversation
      {
         Id = z.Id,
         UserId = z.UserId,
         Title = z.Title,
         Created = z.Created
      }).ToEnumerable();
   }

}