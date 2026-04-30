namespace MentalHealthApp.Application.DTOs;

public class RegisterPatientRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class RegisterPatientResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class PatientLoginRequest
{
    public string EmailOrUsername { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
}

public class CheckAvailabilityResponse
{
    public bool UsernameAvailable { get; set; }
    public bool EmailAvailable { get; set; }
}

public class TherapistSetupRequest
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class TherapistLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AdminLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ValidateInvitationResponse
{
    public bool Valid { get; set; }
    public bool Expired { get; set; }
    public bool AlreadyUsed { get; set; }
    public string? TherapistEmail { get; set; }
    public bool EmailAlreadyRegistered { get; set; }
}

public class ClaimInvitationRequest
{
    public string Token { get; set; } = string.Empty;
}
