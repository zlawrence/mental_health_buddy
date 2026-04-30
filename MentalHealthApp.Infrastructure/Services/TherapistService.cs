using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public class TherapistService : ITherapistService
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly ITherapistAccessRepository _therapistAccessRepository;
    private readonly IGuardRailRepository _guardRailRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationMessageRepository _conversationMessageRepository;

    public TherapistService(
        IUserRepository userRepository,
        IPatientProfileRepository patientProfileRepository,
        ITherapistAccessRepository therapistAccessRepository,
        IGuardRailRepository guardRailRepository,
        IConversationRepository conversationRepository,
        IConversationMessageRepository conversationMessageRepository)
    {
        _userRepository = userRepository;
        _patientProfileRepository = patientProfileRepository;
        _therapistAccessRepository = therapistAccessRepository;
        _guardRailRepository = guardRailRepository;
        _conversationRepository = conversationRepository;
        _conversationMessageRepository = conversationMessageRepository;
    }

    public async Task<List<PatientProfileResponse>> GetAssignedPatientsAsync(string therapistId, CancellationToken cancellationToken = default)
    {
        var accessList = await _therapistAccessRepository.GetByTherapistIdAsync(therapistId, cancellationToken: cancellationToken);
        var assignedPatients = new List<PatientProfileResponse>();

        foreach (var access in accessList)
        {
            var user = await _userRepository.GetByIdAsync(access.PatientUserId, cancellationToken);
            if (user == null) continue;

            var profile = await _patientProfileRepository.GetByUserIdAsync(access.PatientUserId, cancellationToken);

            assignedPatients.Add(new PatientProfileResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber,
                TherapistIds = profile?.TherapistUserIds ?? new List<string>(),
                ConversationRetentionDays = profile?.ConversationRetentionDays ?? -1
            });
        }

        return assignedPatients;
    }

    public async Task<PatientProfileResponse> GetPatientProfileAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(therapistId, patientId, cancellationToken);

        var user = await _userRepository.GetByIdAsync(patientId, cancellationToken);
        if (user == null) throw new InvalidOperationException("Patient not found");

        var profile = await _patientProfileRepository.GetByUserIdAsync(patientId, cancellationToken);

        return new PatientProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            PhoneNumber = user.PhoneNumber,
            TherapistIds = profile?.TherapistUserIds ?? new List<string>(),
            ConversationRetentionDays = profile?.ConversationRetentionDays ?? -1
        };
    }

    public async Task<TherapistAccessResponse> GetTherapistAccessAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        var access = await _therapistAccessRepository.GetByTherapistAndPatientAsync(therapistId, patientId, cancellationToken);
        if (access == null || !access.IsActive)
            throw new UnauthorizedAccessException("Therapist does not have access to this patient");

        return new TherapistAccessResponse
        {
            CanViewChats = access.CanViewChats,
            CanManageGuardRails = access.CanManageGuardRails,
        };
    }

    public async Task<List<GuardRailResponse>> GetPatientGuardRailsAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        await EnsureAccessAsync(therapistId, patientId, cancellationToken);
        var guardRails = await _guardRailRepository.GetByPatientAndTherapistAsync(patientId, therapistId, cancellationToken);

        return guardRails.Select(gr => new GuardRailResponse
        {
            Id = gr.Id,
            PatientId = gr.PatientUserId,
            TherapistId = gr.TherapistUserId,
            Keyword = gr.Keyword,
            Action = gr.Action.ToString(),
            Replacement = gr.Replacement,
            IsActive = gr.IsActive,
            CreatedAt = gr.CreatedAt
        }).ToList();
    }

    public async Task<GuardRailResponse> CreateGuardRailAsync(string therapistId, string patientId, TherapistCreateGuardRailRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageGuardRailsAsync(therapistId, patientId, cancellationToken);

        if (!Enum.TryParse<GuardRailAction>(request.Action, true, out var action))
            throw new InvalidOperationException("Invalid action. Must be 'remove' or 'replace'");

        if (action == GuardRailAction.Replace && string.IsNullOrWhiteSpace(request.Replacement))
            throw new InvalidOperationException("Replacement text is required when action is 'replace'");

        var guardRail = new GuardRail
        {
            PatientUserId = patientId,
            TherapistUserId = therapistId,
            Keyword = request.Keyword.Trim(),
            Action = action,
            Replacement = action == GuardRailAction.Replace ? request.Replacement : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _guardRailRepository.AddAsync(guardRail, cancellationToken);

        return new GuardRailResponse
        {
            Id = guardRail.Id,
            PatientId = guardRail.PatientUserId,
            TherapistId = guardRail.TherapistUserId,
            Keyword = guardRail.Keyword,
            Action = guardRail.Action.ToString(),
            Replacement = guardRail.Replacement,
            IsActive = guardRail.IsActive,
            CreatedAt = guardRail.CreatedAt,
        };
    }

    public async Task UpdateGuardRailAsync(string therapistId, string patientId, string guardRailId, UpdateGuardRailRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageGuardRailsAsync(therapistId, patientId, cancellationToken);

        var guardRail = await _guardRailRepository.GetByIdAsync(guardRailId, cancellationToken);
        if (guardRail == null || guardRail.PatientUserId != patientId)
            throw new InvalidOperationException("Guard rail not found or not accessible");

        if (!Enum.TryParse<GuardRailAction>(request.Action, true, out var action))
            throw new InvalidOperationException("Invalid action. Must be 'remove' or 'replace'");

        guardRail.Keyword = request.Keyword;
        guardRail.Action = action;
        guardRail.Replacement = request.Replacement;
        guardRail.IsActive = request.IsActive;
        guardRail.UpdatedAt = DateTime.UtcNow;

        await _guardRailRepository.UpdateAsync(guardRail, cancellationToken);
    }

    public async Task DeleteGuardRailAsync(string therapistId, string patientId, string guardRailId, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageGuardRailsAsync(therapistId, patientId, cancellationToken);

        var guardRail = await _guardRailRepository.GetByIdAsync(guardRailId, cancellationToken);
        if (guardRail == null || guardRail.PatientUserId != patientId)
            throw new InvalidOperationException("Guard rail not found or not accessible");

        await _guardRailRepository.DeleteAsync(guardRailId, cancellationToken);
    }

    public async Task<List<ConversationResponse>> GetPatientConversationsAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        await EnsureCanViewChatsAsync(therapistId, patientId, cancellationToken);
        var conversations = await _conversationRepository.GetByPatientIdAsync(patientId, includeArchived: true, cancellationToken: cancellationToken);

        return conversations.Select(c => new ConversationResponse
        {
            Id = c.Id,
            PatientId = c.PatientUserId,
            Title = c.Title,
            StartedDate = c.StartedDate,
            IsArchived = c.IsArchived,
            MessageCount = c.MessageCount,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<List<MessageResponse>> GetPatientConversationMessagesAsync(string therapistId, string patientId, string conversationId, CancellationToken cancellationToken = default)
    {
        await EnsureCanViewChatsAsync(therapistId, patientId, cancellationToken);

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null || conversation.PatientUserId != patientId)
            throw new InvalidOperationException("Conversation not found or not accessible");

        var messages = await _conversationMessageRepository.GetByConversationIdAsync(conversationId, cancellationToken);

        return messages.Select(m => new MessageResponse
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role.ToString(),
            Content = m.Content,
            Timestamp = m.Timestamp,
        }).ToList();
    }

    private async Task EnsureAccessAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        var access = await _therapistAccessRepository.GetByTherapistAndPatientAsync(therapistId, patientId, cancellationToken);
        if (access == null || !access.IsActive)
            throw new UnauthorizedAccessException("Therapist does not have access to this patient");
    }

    private async Task EnsureCanManageGuardRailsAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        var access = await _therapistAccessRepository.GetByTherapistAndPatientAsync(therapistId, patientId, cancellationToken);
        if (access == null || !access.IsActive)
            throw new UnauthorizedAccessException("Therapist does not have access to this patient");
        if (!access.CanManageGuardRails)
            throw new UnauthorizedAccessException("Therapist does not have permission to manage guard rails for this patient");
    }

    private async Task EnsureCanViewChatsAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        var access = await _therapistAccessRepository.GetByTherapistAndPatientAsync(therapistId, patientId, cancellationToken);
        if (access == null || !access.IsActive)
            throw new UnauthorizedAccessException("Therapist does not have access to this patient");
        if (!access.CanViewChats)
            throw new UnauthorizedAccessException("Therapist does not have permission to view conversations for this patient");
    }
}
