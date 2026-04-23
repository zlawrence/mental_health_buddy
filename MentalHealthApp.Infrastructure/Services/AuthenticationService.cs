using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly ITherapistInvitationRepository _invitationRepository;
    private readonly ITherapistAccessRepository _therapistAccessRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationService(
        IUserRepository userRepository,
        IPatientProfileRepository patientProfileRepository,
        ITherapistInvitationRepository invitationRepository,
        ITherapistAccessRepository therapistAccessRepository,
        ISubscriptionRepository subscriptionRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _patientProfileRepository = patientProfileRepository;
        _invitationRepository = invitationRepository;
        _therapistAccessRepository = therapistAccessRepository;
        _subscriptionRepository = subscriptionRepository;
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
    }

    private static void ValidatePasswordComplexity(string password)
    {
        if (password.Length < 12 || password.Length > 16)
            throw new InvalidOperationException("Password must be 12–16 characters long");
        if (!password.Any(char.IsUpper))
            throw new InvalidOperationException("Password must contain at least 1 uppercase letter");
        if (!password.Any(char.IsLower))
            throw new InvalidOperationException("Password must contain at least 1 lowercase letter");
        if (password.Count(c => !char.IsLetterOrDigit(c)) < 2)
            throw new InvalidOperationException("Password must contain at least 2 special characters");
    }

    public async Task<RegisterPatientResponse> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePasswordComplexity(request.Password);

        // Check if email already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already registered");
        }

        // Create new user
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = _passwordHashingService.HashPassword(request.Password),
            Role = UserRole.Patient,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        var registeredUser = await _userRepository.AddAsync(user, cancellationToken);

        // Create patient profile
        var patientProfile = new PatientProfile
        {
            PatientUserId = registeredUser.Id,
            ConversationRetentionDays = -1 // Default: forever
        };

        await _patientProfileRepository.AddAsync(patientProfile, cancellationToken);

        return new RegisterPatientResponse
        {
            UserId = registeredUser.Id,
            Email = registeredUser.Email,
            Username = registeredUser.Username
        };
    }

    public async Task<LoginResponse> LoginPatientAsync(PatientLoginRequest request, CancellationToken cancellationToken = default)
    {
        // Find user by email or username
        var user = await _userRepository.GetByEmailAsync(request.EmailOrUsername, cancellationToken);
        user ??= await _userRepository.GetByUsernameAsync(request.EmailOrUsername, cancellationToken);

        if (user == null || user.Role != UserRole.Patient || !user.IsActive)
        {
            throw new InvalidOperationException("Invalid email/username or password");
        }

        // Verify password
        if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email/username or password");
        }

        // Generate JWT
        var token = _jwtTokenService.GenerateToken(user.Id, user.Role.ToString(), user.Email);

        var subscription = await _subscriptionRepository.GetByPatientUserIdAsync(user.Id, cancellationToken);
        var requiresSubscription = subscription == null || subscription.Status != SubscriptionStatus.Active;

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Token = token,
            RequiresSubscription = requiresSubscription
        };
    }

    public async Task<CheckAvailabilityResponse> CheckAvailabilityAsync(string? username, string? email, CancellationToken cancellationToken = default)
    {
        var usernameAvailable = string.IsNullOrEmpty(username) ||
            await _userRepository.GetByUsernameAsync(username, cancellationToken) == null;

        var emailAvailable = string.IsNullOrEmpty(email) ||
            await _userRepository.GetByEmailAsync(email, cancellationToken) == null;

        return new CheckAvailabilityResponse
        {
            UsernameAvailable = usernameAvailable,
            EmailAvailable = emailAvailable
        };
    }

    public async Task<LoginResponse> LoginTherapistAsync(TherapistLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user == null || user.Role != UserRole.Therapist || !user.IsActive)
        {
            throw new InvalidOperationException("Invalid username or password");
        }

        if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password");
        }

        var token = _jwtTokenService.GenerateToken(user.Id, user.Role.ToString(), user.Email);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Token = token
        };
    }

    public async Task<LoginResponse> LoginAdminAsync(AdminLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user == null || user.Role != UserRole.Admin || !user.IsActive)
        {
            throw new InvalidOperationException("Invalid username or password");
        }

        if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password");
        }

        var token = _jwtTokenService.GenerateToken(user.Id, user.Role.ToString(), user.Email);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Token = token
        };
    }

    public async Task<LoginResponse> SetupTherapistAsync(TherapistSetupRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Passwords do not match");
        }

        var invitation = await _invitationRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (invitation == null || invitation.IsUsed || invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired therapist invitation token");
        }

        if (await _userRepository.GetByUsernameAsync(request.Username, cancellationToken) != null)
        {
            throw new InvalidOperationException("Username already taken");
        }

        var therapist = new User
        {
            Email = invitation.TherapistEmail,
            Username = request.Username,
            PasswordHash = _passwordHashingService.HashPassword(request.Password),
            Role = UserRole.Therapist,
            IsActive = true
        };

        var createdTherapist = await _userRepository.AddAsync(therapist, cancellationToken);

        invitation.IsUsed = true;
        invitation.UsedBy = createdTherapist.Id;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        var access = new TherapistAccess
        {
            TherapistUserId = createdTherapist.Id,
            PatientUserId = invitation.PatientUserId,
            CanViewChats = true,
            CanManageGuardRails = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _therapistAccessRepository.AddAsync(access, cancellationToken);

        var profile = await _patientProfileRepository.GetByUserIdAsync(invitation.PatientUserId, cancellationToken);
        if (profile == null)
        {
            profile = new PatientProfile
            {
                PatientUserId = invitation.PatientUserId,
                ConversationRetentionDays = -1
            };
            profile.TherapistUserIds.Add(createdTherapist.Id);
            await _patientProfileRepository.AddAsync(profile, cancellationToken);
        }
        else if (!profile.TherapistUserIds.Contains(createdTherapist.Id))
        {
            profile.TherapistUserIds.Add(createdTherapist.Id);
            profile.UpdatedAt = DateTime.UtcNow;
            await _patientProfileRepository.UpdateAsync(profile, cancellationToken);
        }

        var token = _jwtTokenService.GenerateToken(createdTherapist.Id, createdTherapist.Role.ToString(), createdTherapist.Email);

        return new LoginResponse
        {
            UserId = createdTherapist.Id,
            Email = createdTherapist.Email,
            Username = createdTherapist.Username,
            Token = token
        };
    }
}
