using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class PatientProfileRepository : MongoRepository<PatientProfile>, IPatientProfileRepository
{
    public PatientProfileRepository(IMongoCollection<PatientProfile> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger)
    {
    }

    public async Task<PatientProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatientProfile>.Filter.Eq(p => p.PatientUserId, userId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
