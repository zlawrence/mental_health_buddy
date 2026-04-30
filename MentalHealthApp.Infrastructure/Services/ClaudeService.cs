using Anthropic;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Fx;
using MentalHealthApp.Infrastructure.Resilience;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Polly;

namespace MentalHealthApp.Infrastructure.Services;

public class ClaudeService : ILLMTherapyService
{
    private readonly string _apiKey;
    private readonly string _modelId;
    private readonly ResiliencePipeline _pipeline;
    private const int MaxTokens = 500;

    public ClaudeService(string apiKey, string modelId, ApiResiliencePipelineProvider pipelineProvider)
    {
        _apiKey = apiKey;
        _modelId = modelId;
        _pipeline = pipelineProvider.GetPipeline("Anthropic");
    }

    public async Task<ChatResponse> GetTherapyResponseAsync(
        ChatRequest request,
        List<(string role, string content)> conversationHistory,
        List<string> guardRailInstructions,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(guardRailInstructions);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        foreach (var message in conversationHistory)
        {
            if (message.role == "user")
                chatHistory.AddUserMessage(message.content);
            else
                chatHistory.AddAssistantMessage(message.content);
        }

        chatHistory.AddUserMessage(request.Message);

        var kernel = Kernel.CreateBuilder().Build();
        var service = new AnthropicChatCompletionService(_apiKey, _modelId, MaxTokens);
        var promptSettings = new PromptExecutionSettings
        {
            ServiceId = "anthropic-chat",
            ModelId = _modelId,
        };

        ChatMessageContent responseContent;

        try
        {
            responseContent = await _pipeline.ExecuteAsync(
                ct => new ValueTask<ChatMessageContent>(
                    service.GetChatMessageContentAsync(chatHistory, promptSettings, kernel, ct)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Failure to retrieve messages for the conversation", ex);
        }

        if (responseContent == null || string.IsNullOrWhiteSpace(responseContent.Content))
        {
            return new ChatResponse
            {
                Message = string.Empty,
                RiskLevel = RiskLevel.Normal
            };
        }

        return new ChatResponse
        {
            Message = responseContent.Content.Trim(),
            RiskLevel = RiskLevel.Normal
        };
    }

    private static string BuildSystemPrompt(List<string> guardRailInstructions)
    {
        var basePrompt = @"You are a compassionate mental health support AI designed to help patients with anxiety, depression, and daily emotional challenges. Your role is to:

1. Listen actively and validate the user's feelings
2. Provide supportive, empathetic responses
3. Offer practical coping strategies when appropriate
4. Encourage professional help when needed
5. Never provide medical diagnosis or substitute for professional mental healthcare

If the user asks a question that isn't primarily emotional (e.g., factual questions about geography), politely acknowledge that you're a mental health AI and not an expert in that domain, but try to connect it back to their emotional wellbeing if relevant.

IMPORTANT: Treat all conversations with confidentiality and care. Your goal is to help the patient feel less alone and supported.";

        if (guardRailInstructions.Any())
        {
            var guardRailSection = "\n\n=== PATIENT-SPECIFIC GUARD RAILS (CRITICAL) ===\n";
            guardRailSection += string.Join("\n", guardRailInstructions.Select((g, i) => $"{i + 1}. {g}"));
            basePrompt += guardRailSection;
        }

        return basePrompt;
    }
}
