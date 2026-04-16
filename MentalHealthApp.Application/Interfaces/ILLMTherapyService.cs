using MentalHealthApp.Fx;

namespace MentalHealthApp.Application.Services;

public interface ILLMTherapyService
{
    Task<ChatResponse> GetTherapyResponseAsync(
        ChatRequest request,
        List<(string role, string content)> conversationHistory,
        List<string> guardRailInstructions,
        CancellationToken cancellationToken = default);
}
