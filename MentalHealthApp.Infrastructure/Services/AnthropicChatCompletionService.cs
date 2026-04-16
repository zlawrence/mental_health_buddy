using System.Collections.ObjectModel;
using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;

namespace MentalHealthApp.Infrastructure.Services;

internal sealed class AnthropicChatCompletionService : IChatCompletionService
{
    private const string ServiceIdentifier = "anthropic-chat";
    private readonly string _apiKey;
    private readonly string _modelId;
    private readonly int _maxTokens;
    private readonly IReadOnlyDictionary<string, object?> _attributes;

    public AnthropicChatCompletionService(string apiKey, string modelId, int maxTokens)
    {
        _apiKey = apiKey;
        _modelId = modelId;
        _maxTokens = maxTokens;

        _attributes = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>
        {
            ["serviceId"] = ServiceIdentifier,
            ["modelId"] = modelId,
            ["provider"] = "Anthropic"
        });
    }

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? settings,
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        var systemPrompt = BuildSystemPrompt(chatHistory);
        var messages = ConvertHistoryToAnthropicMessages(chatHistory);
        var model = !string.IsNullOrWhiteSpace(settings?.ModelId) ? settings.ModelId! : _modelId;

        var client = new AnthropicClient(new Anthropic.Core.ClientOptions
        {
            ApiKey = _apiKey,
        });

        var response = await client.Messages.Create(
            new MessageCreateParams
            {
                Model = model,
                MaxTokens = _maxTokens,
                System = systemPrompt,
                Messages = messages,
            },
            cancellationToken);

        var text = ExtractResponseText(response);
        var content = CreateAssistantContent(text);

        return new[] { content };
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? settings,
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Streaming chat is not supported by the Anthropic chat completion service.");
    }

    private static string BuildSystemPrompt(ChatHistory history)
    {
        var systemMessages = history
            .Where(m => m.Role == AuthorRole.System || m.Role == AuthorRole.Developer)
            .Select(m => m.Content?.Trim())
            .Where(content => !string.IsNullOrWhiteSpace(content));

        return string.Join("\n", systemMessages);
    }

    private static IReadOnlyList<MessageParam> ConvertHistoryToAnthropicMessages(ChatHistory history)
    {
        var messages = new List<MessageParam>();

        foreach (var message in history)
        {
            if (message.Role == AuthorRole.System || message.Role == AuthorRole.Developer)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            messages.Add(new MessageParam
            {
                Role = message.Role == AuthorRole.Assistant ? Role.Assistant : Role.User,
                Content = message.Content,
            });
        }

        return messages;
    }

    private static string ExtractResponseText(Message response)
    {
        if (response.Content.Count > 0 && response.Content[0].TryPickText(out var textBlock))
        {
            return textBlock.Text?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private static ChatMessageContent CreateAssistantContent(string text)
    {
        return new ChatMessageContent(
            AuthorRole.Assistant,
            text,
            string.Empty,
            null,
            Encoding.UTF8,
            new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>()));
    }
}
