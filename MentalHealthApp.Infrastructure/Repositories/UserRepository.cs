using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Repositories;

public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(IMongoCollection<User> collection, IResilienceAuditLogger auditLogger) : base(collection, auditLogger)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.IsActive, true);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email);
        var count = await _collection.CountDocumentsAsync(filter, null, cancellationToken);
        return count > 0;
    }
}
