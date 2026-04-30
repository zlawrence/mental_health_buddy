using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class TherapistInvitationRepository : MongoRepository<TherapistInvitation>, ITherapistInvitationRepository
{
    public TherapistInvitationRepository(IMongoCollection<TherapistInvitation> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger)
    {
    }

    public async Task<TherapistInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TherapistInvitation>.Filter.Eq(t => t.Token, token);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TherapistInvitation>> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TherapistInvitation>.Filter.Eq(t => t.PatientUserId, patientId);
        return await _collection.Find(filter)
            .Sort(Builders<TherapistInvitation>.Sort.Descending(t => t.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TherapistInvitation?> GetActiveByPatientAndEmailAsync(string patientId, string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TherapistInvitation>.Filter.And(
            Builders<TherapistInvitation>.Filter.Eq(t => t.PatientUserId, patientId),
            Builders<TherapistInvitation>.Filter.Eq(t => t.TherapistEmail, email),
            Builders<TherapistInvitation>.Filter.Eq(t => t.IsUsed, false),
            Builders<TherapistInvitation>.Filter.Gt(t => t.ExpiresAt, DateTime.UtcNow));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
