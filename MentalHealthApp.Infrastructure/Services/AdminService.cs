using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHashingService _passwordHashingService;

    public AdminService(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHashingService passwordHashingService)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHashingService = passwordHashingService;
    }

    public async Task<List<UserManagementResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetActiveUsersAsync(cancellationToken);
        return users.Select(user => new UserManagementResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role.ToString(),
            IsActive = user.IsActive
        }).ToList();
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, string adminId, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Passwords do not match");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        user.PasswordHash = _passwordHashingService.HashPassword(request.Password);
        await _userRepository.UpdateAsync(user, cancellationToken);

        await LogActionAsync(adminId, "ResetPassword", request.UserId, user.Role.ToString(), null, cancellationToken);
    }

    public async Task ChangePatientStatusAsync(ChangePatientStatusRequest request, string adminId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (user == null || user.Role != UserRole.Patient)
        {
            throw new InvalidOperationException("Patient not found");
        }

        user.IsActive = request.IsActive;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var action = request.IsActive ? "ActivatePatient" : "DeactivatePatient";
        await LogActionAsync(adminId, action, request.PatientId, "Patient", request.Reason, cancellationToken);
    }

    public async Task<UserManagementResponse> AddAdminAsync(AddAdminRequest request, string adminId, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Passwords do not match");
        }

        if (await _userRepository.GetByUsernameAsync(request.Username, cancellationToken) != null || await _userRepository.GetByEmailAsync(request.Email, cancellationToken) != null)
        {
            throw new InvalidOperationException("Username or email already taken");
        }

        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = _passwordHashingService.HashPassword(request.Password),
            Role = UserRole.Admin,
            IsActive = true
        };

        var createdAdmin = await _userRepository.AddAsync(user, cancellationToken);
        await LogActionAsync(adminId, "CreateAdmin", createdAdmin.Id, "Admin", null, cancellationToken);

        return new UserManagementResponse
        {
            UserId = createdAdmin.Id,
            Email = createdAdmin.Email,
            Username = createdAdmin.Username,
            Role = createdAdmin.Role.ToString(),
            IsActive = createdAdmin.IsActive
        };
    }

    public async Task<List<AuditLogResponse>> GetAuditLogsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        List<AuditLog> logs;
        if (from.HasValue && to.HasValue)
        {
            logs = await _auditLogRepository.GetByDateRangeAsync(from.Value, to.Value, cancellationToken);
        }
        else
        {
            logs = await _auditLogRepository.GetAllAsync(cancellationToken);
        }

        return logs.Select(log => new AuditLogResponse
        {
            Id = log.Id,
            AdminId = log.AdminId,
            Action = log.Action,
            TargetId = log.TargetId,
            TargetType = log.TargetType,
            Details = log.Details,
            Timestamp = log.Timestamp
        }).ToList();
    }

    private async Task LogActionAsync(string adminId, string action, string? targetId, string? targetType, string? details, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            AdminId = adminId,
            Action = action,
            TargetId = targetId,
            TargetType = targetType,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }
}
