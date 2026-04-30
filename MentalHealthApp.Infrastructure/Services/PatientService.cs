using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IGuardRailRepository _guardRailRepository;
    private readonly ITherapistInvitationRepository _invitationRepository;
    private readonly IEmailQueue _emailQueue;
    private readonly string _appBaseUrl;

    public PatientService(
        IUserRepository userRepository,
        IPatientProfileRepository patientProfileRepository,
        IGuardRailRepository guardRailRepository,
        ITherapistInvitationRepository invitationRepository,
        IEmailQueue emailQueue,
        string appBaseUrl)
    {
        _userRepository = userRepository;
        _patientProfileRepository = patientProfileRepository;
        _guardRailRepository = guardRailRepository;
        _invitationRepository = invitationRepository;
        _emailQueue = emailQueue;
        _appBaseUrl = appBaseUrl;
    }

    public async Task<PatientProfileResponse> GetPatientProfileAsync(string patientId)
    {
        var user = await _userRepository.GetByIdAsync(patientId);
        if (user == null || user.Role != UserRole.Patient)
        {
            throw new Exception("Patient not found");
        }

        var profile = await _patientProfileRepository.GetByUserIdAsync(patientId);
        if (profile == null)
        {
            profile = new PatientProfile
            {
                PatientUserId = patientId,
                ConversationRetentionDays = -1
            };

            await _patientProfileRepository.AddAsync(profile);
        }

        return new PatientProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            PhoneNumber = user.PhoneNumber,
            TherapistIds = profile.TherapistUserIds,
            ConversationRetentionDays = profile.ConversationRetentionDays
        };
    }

    public async Task UpdatePatientProfileAsync(string patientId, UpdatePatientProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(patientId);
        if (user == null || user.Role != UserRole.Patient)
        {
            throw new Exception("Patient not found");
        }

        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = request.PhoneNumber;
            await _userRepository.UpdateAsync(user);
        }

        if (request.ConversationRetentionDays >= -1)
        {
            var profile = await _patientProfileRepository.GetByUserIdAsync(patientId);
            if (profile == null)
            {
                profile = new PatientProfile
                {
                    PatientUserId = patientId,
                    ConversationRetentionDays = request.ConversationRetentionDays
                };

                await _patientProfileRepository.AddAsync(profile);
            }
            else
            {
                profile.ConversationRetentionDays = request.ConversationRetentionDays;
                profile.UpdatedAt = DateTime.UtcNow;
                await _patientProfileRepository.UpdateAsync(profile);
            }
        }
    }

    public async Task<List<GuardRailResponse>> GetPatientGuardRailsAsync(string patientId)
    {
        var guardRails = await _guardRailRepository.GetByPatientUserIdAsync(patientId);
        return guardRails.Select(gr => new GuardRailResponse
        {
            Id = gr.Id,
            PatientId = gr.PatientUserId,
            TherapistId = gr.TherapistUserId,
            Keyword = gr.Keyword,
            Action = gr.Action.ToString().ToLower(),
            Replacement = gr.Replacement,
            IsActive = gr.IsActive,
            CreatedAt = gr.CreatedAt
        }).ToList();
    }

    public async Task<GuardRailResponse> CreateGuardRailAsync(string patientId, CreateGuardRailRequest request)
    {
        // Verify patient exists
        var patient = await _userRepository.GetByIdAsync(patientId);
        if (patient == null || patient.Role != UserRole.Patient)
        {
            throw new Exception("Patient not found");
        }

        GuardRailAction action;
        if (!Enum.TryParse(request.Action, true, out action))
        {
            throw new Exception("Invalid action. Must be 'remove' or 'replace'");
        }

        var profile = await _patientProfileRepository.GetByUserIdAsync(patientId);
        var therapistId = profile?.TherapistUserIds.FirstOrDefault() ?? string.Empty;

        var guardRail = new GuardRail
        {
            PatientUserId = patientId,
            TherapistUserId = therapistId,
            Keyword = request.Keyword,
            Action = action,
            Replacement = request.Replacement,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _guardRailRepository.AddAsync(guardRail);

        return new GuardRailResponse
        {
            Id = guardRail.Id,
            PatientId = guardRail.PatientUserId,
            TherapistId = guardRail.TherapistUserId,
            Keyword = guardRail.Keyword,
            Action = guardRail.Action.ToString().ToLower(),
            Replacement = guardRail.Replacement,
            IsActive = guardRail.IsActive,
            CreatedAt = guardRail.CreatedAt
        };
    }

    public async Task UpdateGuardRailAsync(string guardRailId, UpdateGuardRailRequest request)
    {
        var guardRail = await _guardRailRepository.GetByIdAsync(guardRailId);
        if (guardRail == null)
        {
            throw new Exception("Guard rail not found");
        }

        GuardRailAction action;
        if (!Enum.TryParse(request.Action, true, out action))
        {
            throw new Exception("Invalid action. Must be 'remove' or 'replace'");
        }

        guardRail.Keyword = request.Keyword;
        guardRail.Action = action;
        guardRail.Replacement = request.Replacement;
        guardRail.IsActive = request.IsActive;

        await _guardRailRepository.UpdateAsync(guardRail);
    }

    public async Task DeleteGuardRailAsync(string guardRailId)
    {
        await _guardRailRepository.DeleteAsync(guardRailId);
    }

    public async Task<TherapistInvitationResponse> InviteTherapistAsync(string patientUserId, InviteTherapistRequest request)
    {
        // Verify patient exists
        var patient = await _userRepository.GetByIdAsync(patientUserId);
        if (patient == null || patient.Role != UserRole.Patient)
        {
            throw new Exception("Patient not found");
        }

        var invitation = new TherapistInvitation
        {
            PatientUserId = patientUserId,
            TherapistEmail = request.TherapistEmail,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _invitationRepository.AddAsync(invitation);

        var setupUrl = $"{_appBaseUrl}/therapist-setup?token={invitation.Token}";
        await _emailQueue.EnqueueAsync(BuildInvitationEmail(
            patient.Username,
            invitation.TherapistEmail,
            setupUrl,
            invitation.ExpiresAt));

        return new TherapistInvitationResponse
        {
            Id = invitation.Id,
            TherapistEmail = invitation.TherapistEmail,
            ExpiresAt = invitation.ExpiresAt,
            IsUsed = invitation.IsUsed
        };
    }

    private static EmailMessage BuildInvitationEmail(
        string patientUsername,
        string therapistEmail,
        string setupUrl,
        DateTime expiresAt)
    {
        return new EmailMessage
        {
            To = therapistEmail,
            Subject = "You've been invited to join Anxiety Buddy",
            HtmlBody = $"""
                <h2>Therapist Invitation — Anxiety Buddy</h2>
                <p>Your patient <strong>{patientUsername}</strong> has invited you to join Anxiety Buddy as their therapist.</p>
                <p>Click the button below to set up your account. This invitation expires on <strong>{expiresAt:MMMM d, yyyy}</strong>.</p>
                <p style="margin:24px 0;">
                  <a href="{setupUrl}"
                     style="background:#6f5acd;color:#fff;padding:12px 24px;border-radius:4px;text-decoration:none;font-weight:bold;">
                    Accept Invitation
                  </a>
                </p>
                <p style="color:#888;font-size:12px;">
                  If the button doesn't work, copy this link into your browser:<br>
                  <a href="{setupUrl}">{setupUrl}</a>
                </p>
                <hr>
                <p style="color:#888;font-size:12px;">Anxiety Buddy — AI-assisted mental health support</p>
                """,
            TextBody = $"""
                Therapist Invitation — Anxiety Buddy

                Your patient {patientUsername} has invited you to join Anxiety Buddy as their therapist.

                Set up your account here (expires {expiresAt:MMMM d, yyyy}):
                {setupUrl}

                Anxiety Buddy — AI-assisted mental health support
                """
        };
    }
}