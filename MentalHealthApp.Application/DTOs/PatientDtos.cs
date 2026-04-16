namespace MentalHealthApp.Application.DTOs;

// Conversation DTOs
public class CreateConversationRequest
{
    public string Title { get; set; } = string.Empty;
}

public class ConversationResponse
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartedDate { get; set; }
    public bool IsArchived { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendMessageRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class MessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class RateLimitWarning
{
    public bool IsAtLimit { get; set; }
    public int CurrentCount { get; set; }
    public int MaxAllowed { get; set; }
    public string? Message { get; set; }
}

// Guard Rail DTOs
public class CreateGuardRailRequest
{
    public string PatientId { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "remove" or "replace"
    public string? Replacement { get; set; }
}

public class UpdateGuardRailRequest
{
    public string Keyword { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Replacement { get; set; }
    public bool IsActive { get; set; } = true;
}

public class GuardRailResponse
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string TherapistId { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Replacement { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Patient Profile DTOs
public class UpdatePatientProfileRequest
{
    public string? PhoneNumber { get; set; }
    public int ConversationRetentionDays { get; set; } = -1; // -1 = forever
}

public class PatientProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public List<string> TherapistIds { get; set; } = new();
    public int ConversationRetentionDays { get; set; }
}

// Therapist Invitation DTOs
public class InviteTherapistRequest
{
    public string TherapistEmail { get; set; } = string.Empty;
}

public class TherapistInvitationResponse
{
    public string Id { get; set; } = string.Empty;
    public string TherapistEmail { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}

// Emergency Contact DTOs
public class AddEmergencyContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? SmsNumber { get; set; }
    public bool IsPrimary { get; set; } = false;
}

public class EmergencyContactResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? SmsNumber { get; set; }
    public bool IsPrimary { get; set; }
}
