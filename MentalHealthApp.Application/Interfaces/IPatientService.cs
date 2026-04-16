using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IPatientService
{
    Task<PatientProfileResponse> GetPatientProfileAsync(string patientId);
    Task UpdatePatientProfileAsync(string patientId, UpdatePatientProfileRequest request);
    Task<List<GuardRailResponse>> GetPatientGuardRailsAsync(string patientId);
    Task<GuardRailResponse> CreateGuardRailAsync(string patientId, CreateGuardRailRequest request);
    Task UpdateGuardRailAsync(string guardRailId, UpdateGuardRailRequest request);
    Task DeleteGuardRailAsync(string guardRailId);
    Task<TherapistInvitationResponse> InviteTherapistAsync(string patientId, InviteTherapistRequest request);
}
