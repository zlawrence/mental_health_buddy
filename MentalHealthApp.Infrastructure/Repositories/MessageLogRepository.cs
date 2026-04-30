using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class MessageLogRepository : MongoRepository<MessageLog>, IMessageLogRepository
{
    public MessageLogRepository(IMongoCollection<MessageLog> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger) { }

    public async Task<List<MessageLog>> GetByStatusAsync(MessageLogStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageLog>.Filter.Eq(m => m.Status, status);
        return await _collection.Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MessageLog>> GetByTypeAsync(MessageType messageType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageLog>.Filter.Eq(m => m.MessageType, messageType);
        return await _collection.Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
