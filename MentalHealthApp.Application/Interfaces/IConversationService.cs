using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IConversationService
{
    Task<ConversationResponse> CreateConversationAsync(string patientId, CreateConversationRequest request, CancellationToken cancellationToken = default);
    Task<List<ConversationResponse>> GetPatientConversationsAsync(string patientId, CancellationToken cancellationToken = default);
    Task<ConversationResponse> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    Task UpdateConversationAsync(string conversationId, bool isArchived, CancellationToken cancellationToken = default);
    Task<MessageResponse> SendMessageAsync(string patientId, SendMessageRequest request, CancellationToken cancellationToken = default);
    Task<List<MessageResponse>> GetConversationMessagesAsync(string conversationId, CancellationToken cancellationToken = default);
}
