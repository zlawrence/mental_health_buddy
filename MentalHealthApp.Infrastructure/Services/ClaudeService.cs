using Anthropic;
using Anthropic.Models.Messages;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Fx;

namespace MentalHealthApp.Infrastructure.Services;

public class ClaudeService : ILLMTherapyService
{
    private readonly string _apiKey;
    private readonly string _modelId;
    private const int MaxTokens = 500;

    public ClaudeService(string apiKey, string modelId)
    {
        _apiKey = apiKey;
        _modelId = modelId;
    }

    public async Task<ChatResponse> GetTherapyResponseAsync(
        ChatRequest request,
        List<(string role, string content)> conversationHistory,
        List<string> guardRailInstructions,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(guardRailInstructions);

        var client = new AnthropicClient(new Anthropic.Core.ClientOptions
        { 
            ApiKey = _apiKey,
        });

        var messages = conversationHistory
            .Select(m => new MessageParam
            {
                Role = m.role == "user" ? Role.User : Role.Assistant,
                Content = m.content,
            })
            .Append(new MessageParam
            {
                Role = Role.User,
                Content = request.Message,
            })
            .ToList();

        Message response;

        try
        {
            response = await client.Messages.Create(
                new MessageCreateParams
                {
                    Model = _modelId,
                    MaxTokens = MaxTokens,
                    System = systemPrompt,
                    Messages = messages,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Failure to retrieve messages for the conversation", ex);
        }

        if (response.Content.Count == 0 ||
            !response.Content[0].TryPickText(out var textBlock))
        {
            return new ChatResponse
            {
                Message = string.Empty,
                RiskLevel = RiskLevel.Normal
            };
        }

        return new ChatResponse
        {
            Message = textBlock.Text,
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
