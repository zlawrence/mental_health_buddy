using MongoDB.Driver;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Repositories;

public class EmergencyContactRepository : MongoRepository<EmergencyContact>, IEmergencyContactRepository
{
    public EmergencyContactRepository(IMongoCollection<EmergencyContact> collection) : base(collection)
    {
    }

    public async Task<List<EmergencyContact>> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EmergencyContact>.Filter.Eq(e => e.PatientUserId, patientId);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<EmergencyContact?> GetPrimaryByPatientIdAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EmergencyContact>.Filter.And(
            Builders<EmergencyContact>.Filter.Eq(e => e.PatientUserId, patientId),
            Builders<EmergencyContact>.Filter.Eq(e => e.IsPrimary, true));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteByPatientIdAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EmergencyContact>.Filter.Eq(e => e.PatientUserId, patientId);
        await _collection.DeleteManyAsync(filter, null, cancellationToken);
    }
}
