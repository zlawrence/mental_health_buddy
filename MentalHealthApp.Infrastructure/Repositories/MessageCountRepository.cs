using MongoDB.Driver;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Repositories;

public class MessageCountRepository : MongoRepository<MessageCount>, IMessageCountRepository
{
    public MessageCountRepository(IMongoCollection<MessageCount> collection) : base(collection)
    {
    }

    public async Task<MessageCount?> GetByPatientAndDateAsync(string patientId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dateOnly = date.Date;
        var filter = Builders<MessageCount>.Filter.And(
            Builders<MessageCount>.Filter.Eq(m => m.PatientUserId, patientId),
            Builders<MessageCount>.Filter.Gte(m => m.Date, dateOnly),
            Builders<MessageCount>.Filter.Lt(m => m.Date, dateOnly.AddDays(1)));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MessageCount> IncrementCountAsync(string patientId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dateOnly = date.Date;
        var existing = await GetByPatientAndDateAsync(patientId, dateOnly, cancellationToken);

        if (existing != null)
        {
            existing.Count++;
            existing.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(existing, cancellationToken);
            return existing;
        }

        var newCount = new MessageCount
        {
            PatientUserId = patientId,
            Date = dateOnly,
            Count = 1
        };

        await AddAsync(newCount, cancellationToken);
        return newCount;
    }
}
