using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class ConversationMessageRepository : MongoRepository<ConversationMessage>, IConversationMessageRepository
{
    public ConversationMessageRepository(IMongoCollection<ConversationMessage> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger)
    {
    }

    public async Task<List<ConversationMessage>> GetByConversationIdAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ConversationMessage>.Filter.Eq(m => m.ConversationId, conversationId);
        return await _collection.Find(filter)
            .Sort(Builders<ConversationMessage>.Sort.Ascending(m => m.Timestamp))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConversationMessage>> GetPagedByConversationIdAsync(string conversationId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ConversationMessage>.Filter.Eq(m => m.ConversationId, conversationId);
        return await _collection.Find(filter)
            .Sort(Builders<ConversationMessage>.Sort.Descending(m => m.Timestamp))
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByConversationIdAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ConversationMessage>.Filter.Eq(m => m.ConversationId, conversationId);
        await _collection.DeleteManyAsync(filter, null, cancellationToken);
    }
}
