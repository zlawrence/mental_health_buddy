using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class TherapistAccessRepository : MongoRepository<TherapistAccess>, ITherapistAccessRepository
{
    public TherapistAccessRepository(IMongoCollection<TherapistAccess> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger)
    {
    }

    public async Task<List<TherapistAccess>> GetByPatientIdAsync(string patientId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TherapistAccess>.Filter.Eq(t => t.PatientUserId, patientId);
        
        if (activeOnly)
        {
            filter = Builders<TherapistAccess>.Filter.And(
                filter,
                Builders<TherapistAccess>.Filter.Eq(t => t.IsActive, true));
        }

        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<List<TherapistAccess>> GetByTherapistIdAsync(string therapistId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TherapistAccess>.Filter.Eq(t => t.TherapistUserId, therapistId);
        
        if (activeOnly)
        {
            filter = Builders<TherapistAccess>.Filter.And(
                filter,
                Builders<TherapistAccess>.Filter.Eq(t => t.IsActive, true));
        }

        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<TherapistAccess?> GetByTherapistAndPatientAsync(string therapistId, string patientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TherapistAccess>.Filter.And(
            Builders<TherapistAccess>.Filter.Eq(t => t.TherapistUserId, therapistId),
            Builders<TherapistAccess>.Filter.Eq(t => t.PatientUserId, patientId));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
