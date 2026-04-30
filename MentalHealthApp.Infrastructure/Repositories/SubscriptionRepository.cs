using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class SubscriptionRepository : MongoRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(IMongoCollection<Subscription> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger)
    {
    }

    public async Task<Subscription?> GetByPatientUserIdAsync(string patientUserId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Subscription>.Filter.Eq(s => s.PatientUserId, patientUserId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Subscription>.Filter.Eq(s => s.StripeSubscriptionId, stripeSubscriptionId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
