using MongoDB.Driver;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Repositories;

public class AuditLogRepository : MongoRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(IMongoCollection<AuditLog> collection) : base(collection)
    {
    }

    public async Task<List<AuditLog>> GetByAdminIdAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.Eq(a => a.AdminId, adminId);
        return await _collection.Find(filter)
            .Sort(Builders<AuditLog>.Sort.Descending(a => a.Timestamp))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.And(
            Builders<AuditLog>.Filter.Gte(a => a.Timestamp, from),
            Builders<AuditLog>.Filter.Lte(a => a.Timestamp, to));

        return await _collection.Find(filter)
            .Sort(Builders<AuditLog>.Sort.Descending(a => a.Timestamp))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetByTargetIdAsync(string targetId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuditLog>.Filter.Eq(a => a.TargetId, targetId);
        return await _collection.Find(filter)
            .Sort(Builders<AuditLog>.Sort.Descending(a => a.Timestamp))
            .ToListAsync(cancellationToken);
    }
}
