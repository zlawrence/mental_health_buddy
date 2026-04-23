using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IAuthenticationService
{
    Task<RegisterPatientResponse> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginPatientAsync(PatientLoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginTherapistAsync(TherapistLoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAdminAsync(AdminLoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> SetupTherapistAsync(TherapistSetupRequest request, CancellationToken cancellationToken = default);
    Task<CheckAvailabilityResponse> CheckAvailabilityAsync(string? username, string? email, CancellationToken cancellationToken = default);
}
