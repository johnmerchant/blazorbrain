using System.Runtime.CompilerServices;
using Azure.AI.OpenAI;
using BlazorBrain.Application.Abstractions.Chat;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace BlazorBrain.Infrastructure.Chat;

public class ChatService : IChatService
{
    public const string DatabaseName = "BlazorBrain";
    public const string ContainerName = "Conversations";

    public const string PromptMessage =
        "You are a helpful assistant named BlazorBrain. " +
        "Your responses should be rendered as Markdown. " +
        "When the user requests geographical information, it is presented to the user on an Azure Maps component, you should respond with a code block of valid application/geo+json (GeoJSON format) and do not mention this code block to the user as it is passed to the Azure Maps component.";

    public const string TitlePromptMessage = "Create a title for our conversation, with your response only containing the title and do not mention geography or GeoJSON functionality.";
    
    
    private readonly CosmosClient _client;
    private readonly OpenAIClient _openAI;
    private readonly string _deploymentName;

    public ChatService(CosmosClient client, OpenAIClient openAI, IOptions<OpenAIOptions> options)
    {
        _client = client;
        _openAI = openAI;
        _deploymentName = options.Value.DeploymentName;
    }

    private async Task<Container> GetContainer(CancellationToken cancellationToken)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(DatabaseName, cancellationToken: cancellationToken);
        var container = await db.Database.CreateContainerIfNotExistsAsync(ContainerName, "/UserId", cancellationToken: cancellationToken);
        return container.Container;
    }

    public async IAsyncEnumerable<ConversationListing> List(string userId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var container = await GetContainer(cancellationToken);
        var iterator = container.GetItemLinqQueryable<ConversationListing>().Where(z => z.UserId == userId).ToFeedIterator();
        while (iterator.HasMoreResults)
        {
            foreach (var result in await iterator.ReadNextAsync(cancellationToken))
            {
                yield return result;
            }
        }
    }

    public async Task<Conversation> Load(string userId, Guid conversationId, CancellationToken cancellationToken)
    {
        var container = await GetContainer(cancellationToken);
        return await Load(container, userId, conversationId, cancellationToken);
    }

    private async Task<Conversation> Load(Container container, string userId, Guid conversationId, CancellationToken cancellationToken)
    {
        var iterator = container.GetItemLinqQueryable<Conversation>().Where(z => z.UserId == userId && z.Id == conversationId).ToFeedIterator();
        var response = await iterator.ReadNextAsync(cancellationToken);
        return response.Single();
    }

    public async Task<Conversation> Create(string userId, CancellationToken cancellationToken)
    {
        var container = await GetContainer(cancellationToken);
        var conversation = new Conversation(Guid.NewGuid(), userId, "New Chat", Array.Empty<ConversationMessage>(), false, DateTimeOffset.Now);
        var result = await container.CreateItemAsync(conversation, cancellationToken: cancellationToken);
        return result.Resource;
    } 
    
    public async IAsyncEnumerable<Conversation> StreamPrompt(string userId, Guid conversationId, string message, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var container = await GetContainer(cancellationToken);
        var conversation = await Load(container, userId, conversationId, cancellationToken);
        var promptConversationMessage = new ConversationMessage(
            Guid.NewGuid(),
            userId, 
            ConversationRole.System, 
            new[] { PromptMessage }, 
            "New conversation"
        );
        
        var promptUserConversationMessage = new ConversationMessage(Guid.NewGuid(), userId, ConversationRole.User, new[] { message });
        var messages = new[] { promptConversationMessage, promptUserConversationMessage };
        var patchPromptMessageOpts = messages.Select(z => PatchOperation.Add("/Messages/-", z)).ToArray();
        
        await container.PatchItemAsync<Conversation>(
            conversationId.ToString(), 
            new PartitionKey(userId), 
            patchOperations: patchPromptMessageOpts, 
            cancellationToken: cancellationToken
        );
        
        yield return conversation = conversation with { Messages = messages };
        
        var completionMessages = messages.Select(GetChatCompletionMessage).ToArray();
        var completionsOptions = new ChatCompletionsOptions();
        foreach (var completionMessage in completionMessages)
        {
            completionsOptions.Messages.Add(completionMessage);
        }
        var response = await _openAI.GetChatCompletionsStreamingAsync(_deploymentName, completionsOptions, cancellationToken);
        var responseMessage = new ConversationMessage(Guid.NewGuid(), userId, ConversationRole.Bot, Array.Empty<string>());
        yield return conversation with { Messages = messages.Append(responseMessage).ToArray() };
        
        
        await foreach (var streamingChatChoice in response.Value.GetChoicesStreaming(cancellationToken))
        {
            var content = new List<string>();
            await foreach (var chatMessage in streamingChatChoice.GetMessageStreaming(cancellationToken))
            {
                responseMessage = GetConversationMessage(responseMessage.Id, userId, chatMessage, content);
                if (!string.IsNullOrEmpty(chatMessage.Content))
                {
                    content.Add(chatMessage.Content);
                    yield return conversation with { Messages = messages.Append(responseMessage).ToArray(), IsStreaming = true };
                }
            }
            break;
        }
        
        conversation = conversation with
        {
            Messages = messages.Append(responseMessage).ToArray(), 
            IsStreaming = false
        };
        
        await foreach (var titleConversation in SetTitle(conversation, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return conversation = titleConversation;
        }

        
        var patchOperations = new []
        {
            PatchOperation.Add("/Messages/-", responseMessage),
            PatchOperation.Set("/Title", conversation.Title)
        };
        
        await container.PatchItemAsync<Conversation>(conversationId.ToString(), new PartitionKey(userId), patchOperations: patchOperations, cancellationToken: cancellationToken);
        yield return conversation with { Messages = messages.Append(responseMessage).ToArray(), IsStreaming = false };
    }

    public async IAsyncEnumerable<Conversation> StreamResponse(string userId, Guid conversationId, string userMessage, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var container = await GetContainer(cancellationToken);
        var conversation = await Load(container, userId, conversationId, cancellationToken);
        var messages = conversation.Messages.ToList();
        var userConversationMessage = new ConversationMessage(Guid.NewGuid(), userId, ConversationRole.User, new[] { userMessage });
        messages.Add(userConversationMessage);
        var patchUserMessageOperations = new[] { PatchOperation.Add("/Messages/-", userConversationMessage) };
        await container.PatchItemAsync<Conversation>(conversationId.ToString(), new PartitionKey(userId), patchOperations: patchUserMessageOperations, cancellationToken: cancellationToken);
        yield return conversation = conversation with { Messages = messages.ToArray() };
        
        var completionMessages = messages.Select(GetChatCompletionMessage).ToArray();
        var completionsOptions = new ChatCompletionsOptions();
        foreach (var message in completionMessages)
        {
            completionsOptions.Messages.Add(message);
        }
        
        var response = await _openAI.GetChatCompletionsStreamingAsync(_deploymentName, completionsOptions, cancellationToken);
        var responseMessage = new ConversationMessage(Guid.NewGuid(), userId, ConversationRole.Bot, Array.Empty<string>());
        yield return conversation with { Messages = messages.Append(responseMessage).ToArray() };
        await foreach (var choice in response.Value.GetChoicesStreaming(cancellationToken))
        {
            var content = new List<string>();
            await foreach (var message in choice.GetMessageStreaming(cancellationToken))
            {
                responseMessage = GetConversationMessage(responseMessage.Id, userId, message, content);
                if (!string.IsNullOrEmpty(message.Content))
                {
                    content.Add(message.Content);
                    yield return conversation with { Messages = messages.Append(responseMessage).ToArray(), IsStreaming = true };
                    await Task.Delay(Random.Shared.Next(1, 100), cancellationToken);
                }
            }
            break;
        }
        
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Add("/Messages/-", responseMessage) 
        };

        await container.PatchItemAsync<Conversation>(conversationId.ToString(), new PartitionKey(userId), patchOperations: patchOperations, cancellationToken: cancellationToken);
        
        yield return conversation with { Messages = messages.Append(responseMessage).ToArray(), IsStreaming = false };
    }

    private async IAsyncEnumerable<Conversation> SetTitle(Conversation conversation, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var completionMessages = conversation.Messages.Select(GetChatCompletionMessage).Concat(new [] { new ChatMessage("system", TitlePromptMessage) }).ToArray();
        var completionOptions = new ChatCompletionsOptions();
        foreach (var message in completionMessages)
        {
            completionOptions.Messages.Add(message);
        }

        var streamingCompletions = await _openAI.GetChatCompletionsStreamingAsync(_deploymentName, completionOptions, cancellationToken);
        await foreach (var streamingChatChoice in streamingCompletions.Value.GetChoicesStreaming(cancellationToken))
        {
            var content = new List<string>();
            await foreach (var message in streamingChatChoice.GetMessageStreaming(cancellationToken))
            {
                content.Add(message.Content);
                var title = string.Join("", content);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    yield return conversation with { Title = title };
                }
            }
            break;
        }
    }

    public async IAsyncEnumerable<ConversationMessage> LoadMessages(string userId, Guid id, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var container = await GetContainer(cancellationToken);
        var iterator = container
            .GetItemLinqQueryable<Conversation>()
            .Where(z => z.UserId == userId && z.Id == id)
            .SelectMany(z => z.Messages)
            .ToFeedIterator();
        
        while (iterator.HasMoreResults)
        {
            foreach (var message in await iterator.ReadNextAsync(cancellationToken))
            {
                yield return message;
            }
        }
    }

    private static ChatMessage GetChatCompletionMessage(ConversationMessage message) => new (
        message.Role switch
        {
            ConversationRole.Bot => "assistant",
            ConversationRole.User => "user",
            ConversationRole.System => "system",
            _ => throw new ArgumentOutOfRangeException()
        },
        string.Join("", message.Content)
    );
    
    private static ConversationMessage GetConversationMessage(Guid id, string userId, ChatMessage chatMessage, IEnumerable<string> content) => new (
        id,
        userId,
        GetConversationRole(chatMessage.Role),
        content.Append(chatMessage.Content ?? "").ToArray()
    );
    
    private static ConversationRole GetConversationRole(ChatRole role)
    {
        if (role == ChatRole.Assistant) return ConversationRole.Bot;
        if (role == ChatRole.User) return ConversationRole.User;
        if (role == ChatRole.System) return ConversationRole.System;
        throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported role " + role);
    }
}