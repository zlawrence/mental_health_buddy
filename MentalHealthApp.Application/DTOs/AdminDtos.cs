namespace MentalHealthApp.Application.DTOs;

public class AddAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePatientStatusRequest
{
    public string PatientId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}

public class UserManagementResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AuditLogResponse
{
    public string Id { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? TargetType { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
