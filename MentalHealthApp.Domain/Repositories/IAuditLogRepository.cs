using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<List<AuditLog>> GetByAdminIdAsync(string adminId, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetByTargetIdAsync(string targetId, CancellationToken cancellationToken = default);
}
