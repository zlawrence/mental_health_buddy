using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface ITherapistService
{
    Task<List<PatientProfileResponse>> GetAssignedPatientsAsync(string therapistId, CancellationToken cancellationToken = default);
    Task<PatientProfileResponse> GetPatientProfileAsync(string therapistId, string patientId, CancellationToken cancellationToken = default);
    Task<List<GuardRailResponse>> GetPatientGuardRailsAsync(string therapistId, string patientId, CancellationToken cancellationToken = default);
    Task<GuardRailResponse> CreateGuardRailAsync(string therapistId, string patientId, TherapistCreateGuardRailRequest request, CancellationToken cancellationToken = default);
    Task UpdateGuardRailAsync(string therapistId, string patientId, string guardRailId, UpdateGuardRailRequest request, CancellationToken cancellationToken = default);
    Task DeleteGuardRailAsync(string therapistId, string patientId, string guardRailId, CancellationToken cancellationToken = default);
    Task<List<ConversationResponse>> GetPatientConversationsAsync(string therapistId, string patientId, CancellationToken cancellationToken = default);
    Task<List<MessageResponse>> GetPatientConversationMessagesAsync(string therapistId, string patientId, string conversationId, CancellationToken cancellationToken = default);
    Task<TherapistAccessResponse> GetTherapistAccessAsync(string therapistId, string patientId, CancellationToken cancellationToken = default);
}
