using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Resilience;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;

namespace MentalHealthApp.Infrastructure.Repositories;

public abstract class MongoRepository<T> : IRepository<T> where T : class
{
    protected readonly IMongoCollection<T> _collection;
    private readonly IResilienceAuditLogger? _auditLogger;
    private readonly ResiliencePipeline _retryPipeline;

    protected MongoRepository(IMongoCollection<T> collection, IResilienceAuditLogger? auditLogger)
    {
        _collection = collection;
        _auditLogger = auditLogger;
        _retryPipeline = ResiliencePolicies.BuildDbRetryPipeline(auditLogger, typeof(T).Name);
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async ct =>
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<T>.Filter.Eq("_id", objectId);
            return await _collection.Find(filter).FirstOrDefaultAsync(ct);
        }, nameof(GetByIdAsync), cancellationToken);
    }

    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            ct => _collection.Find(_ => true).ToListAsync(ct),
            nameof(GetAllAsync), cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async ct =>
        {
            await _collection.InsertOneAsync(entity, null, ct);
            return entity;
        }, nameof(AddAsync), cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await ExecuteVoidAsync(async ct =>
        {
            var filter = Builders<T>.Filter.Eq("_id", entity.GetType().GetProperty("Id")!.GetValue(entity));
            await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions(), ct);
        }, nameof(UpdateAsync), cancellationToken);
    }

    public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await ExecuteVoidAsync(async ct =>
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            await _collection.DeleteOneAsync(filter, null, ct);
        }, nameof(DeleteAsync), cancellationToken);
    }

    protected async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _retryPipeline.ExecuteAsync(ct => new ValueTask<TResult>(operation(ct)), cancellationToken);
        }
        catch (Exception ex)
        {
            if (_auditLogger is not null)
                await _auditLogger.LogAsync($"DbRetryExhausted:{typeof(T).Name}.{operationName}", ex.Message);
            throw;
        }
    }

    private async Task ExecuteVoidAsync(
        Func<CancellationToken, Task> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        try
        {
            await _retryPipeline.ExecuteAsync(ct => new ValueTask(operation(ct)), cancellationToken);
        }
        catch (Exception ex)
        {
            if (_auditLogger is not null)
                await _auditLogger.LogAsync($"DbRetryExhausted:{typeof(T).Name}.{operationName}", ex.Message);
            throw;
        }
    }
}

