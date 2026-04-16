using MongoDB.Driver;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Repositories;

public class ConversationRepository : MongoRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(IMongoCollection<Conversation> collection) : base(collection)
    {
    }

    public async Task<List<Conversation>> GetByPatientIdAsync(string patientId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.PatientUserId, patientId);
        
        if (!includeArchived)
        {
            filter = Builders<Conversation>.Filter.And(
                filter,
                Builders<Conversation>.Filter.Eq(c => c.IsArchived, false));
        }

        return await _collection.Find(filter)
            .Sort(Builders<Conversation>.Sort.Descending(c => c.StartedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Conversation>> SearchByPatientAndKeywordAsync(string patientId, string keyword, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.PatientUserId, patientId),
            Builders<Conversation>.Filter.Regex(c => c.Title, new MongoDB.Bson.BsonRegularExpression(keyword, "i")));

        return await _collection.Find(filter)
            .Sort(Builders<Conversation>.Sort.Descending(c => c.StartedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetLatestByPatientIdAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.PatientUserId, patientId);
        return await _collection.Find(filter)
            .Sort(Builders<Conversation>.Sort.Descending(c => c.StartedDate))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
