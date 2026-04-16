using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IAdminService
{
    Task<List<UserManagementResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, string adminId, CancellationToken cancellationToken = default);
    Task ChangePatientStatusAsync(ChangePatientStatusRequest request, string adminId, CancellationToken cancellationToken = default);
    Task<UserManagementResponse> AddAdminAsync(AddAdminRequest request, string adminId, CancellationToken cancellationToken = default);
    Task<List<AuditLogResponse>> GetAuditLogsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
