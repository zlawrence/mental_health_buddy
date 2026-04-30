using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class GuardRailRepository : MongoRepository<GuardRail>, IGuardRailRepository
{
    public GuardRailRepository(IMongoCollection<GuardRail> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger)
    {
    }

    public async Task<List<GuardRail>> GetByPatientUserIdAsync(string patientId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GuardRail>.Filter.Eq(g => g.PatientUserId, patientId);
        
        if (activeOnly)
        {
            filter = Builders<GuardRail>.Filter.And(
                filter,
                Builders<GuardRail>.Filter.Eq(g => g.IsActive, true));
        }

        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<List<GuardRail>> GetByPatientAndTherapistAsync(string patientId, string therapistId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GuardRail>.Filter.And(
            Builders<GuardRail>.Filter.Eq(g => g.PatientUserId, patientId),
            Builders<GuardRail>.Filter.Eq(g => g.TherapistUserId, therapistId));

        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task DeleteByPatientUserIdAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GuardRail>.Filter.Eq(g => g.PatientUserId, patientId);
        await _collection.DeleteManyAsync(filter, null, cancellationToken);
    }
}
