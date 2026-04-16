using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IGuardRailService
{
    Task<GuardRailResponse> CreateGuardRailAsync(string therapistId, CreateGuardRailRequest request, CancellationToken cancellationToken = default);
    Task<GuardRailResponse> UpdateGuardRailAsync(string guardRailId, string therapistId, UpdateGuardRailRequest request, CancellationToken cancellationToken = default);
    Task DeleteGuardRailAsync(string guardRailId, string therapistId, CancellationToken cancellationToken = default);
    Task<List<GuardRailResponse>> GetPatientGuardRailsAsync(string patientId, string therapistId, CancellationToken cancellationToken = default);
}
