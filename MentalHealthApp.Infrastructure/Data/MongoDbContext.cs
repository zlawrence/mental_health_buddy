using MongoDB.Driver;

namespace MentalHealthApp.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return _database.GetCollection<T>(collectionName);
    }

    // Collections
    public IMongoCollection<Domain.Entities.User> Users =>
        GetCollection<Domain.Entities.User>("users");

    public IMongoCollection<Domain.Entities.PatientProfile> PatientProfiles =>
        GetCollection<Domain.Entities.PatientProfile>("patient_profiles");

    public IMongoCollection<Domain.Entities.GuardRail> GuardRails =>
        GetCollection<Domain.Entities.GuardRail>("guard_rails");

    public IMongoCollection<Domain.Entities.Conversation> Conversations =>
        GetCollection<Domain.Entities.Conversation>("conversations");

    public IMongoCollection<Domain.Entities.ConversationMessage> ConversationMessages =>
        GetCollection<Domain.Entities.ConversationMessage>("conversation_messages");

    public IMongoCollection<Domain.Entities.EmergencyContact> EmergencyContacts =>
        GetCollection<Domain.Entities.EmergencyContact>("emergency_contacts");

    public IMongoCollection<Domain.Entities.TherapistAccess> TherapistAccess =>
        GetCollection<Domain.Entities.TherapistAccess>("therapist_access");

    public IMongoCollection<Domain.Entities.AuditLog> AuditLogs =>
        GetCollection<Domain.Entities.AuditLog>("audit_logs");

    public IMongoCollection<Domain.Entities.MessageCount> MessageCounts =>
        GetCollection<Domain.Entities.MessageCount>("message_counts");

    public IMongoCollection<Domain.Entities.TherapistInvitation> TherapistInvitations =>
        GetCollection<Domain.Entities.TherapistInvitation>("therapist_invitations");

    public IMongoCollection<Domain.Entities.MessageLog> MessageLogs =>
        GetCollection<Domain.Entities.MessageLog>("message_logs");

    public async Task InitializeIndexesAsync()
    {
        // User indexes
        var usersCollection = Users;
        await usersCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Domain.Entities.User>(
                Builders<Domain.Entities.User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }));

        // PatientProfile indexes
        var profilesCollection = PatientProfiles;
        await profilesCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Domain.Entities.PatientProfile>(
                Builders<Domain.Entities.PatientProfile>.IndexKeys.Ascending(p => p.PatientUserId),
                new CreateIndexOptions { Unique = true }));

        // GuardRail indexes
        var guardRailsCollection = GuardRails;
        await guardRailsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Domain.Entities.GuardRail>(
                Builders<Domain.Entities.GuardRail>.IndexKeys
                    .Ascending(g => g.PatientUserId)
                    .Ascending(g => g.IsActive)));

        // Conversation indexes
        var conversationsCollection = Conversations;
        await conversationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Domain.Entities.Conversation>(
                Builders<Domain.Entities.Conversation>.IndexKeys
                    .Ascending(c => c.PatientUserId)
                    .Descending(c => c.StartedDate)));

        // ConversationMessage indexes
        var messagesCollection = ConversationMessages;
        await messagesCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Domain.Entities.ConversationMessage>(
                Builders<Domain.Entities.ConversationMessage>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Ascending(m => m.Timestamp)));

        // MessageCount indexes
        var messageCountCollection = MessageCounts;
        await messageCountCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Domain.Entities.MessageCount>(
                Builders<Domain.Entities.MessageCount>.IndexKeys
                    .Ascending(m => m.PatientUserId)
                    .Ascending(m => m.Date)));

        // MessageLog indexes
        var messageLogsCollection = MessageLogs;
        await messageLogsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Domain.Entities.MessageLog>(
                Builders<Domain.Entities.MessageLog>.IndexKeys
                    .Ascending(m => m.Status)
                    .Descending(m => m.CreatedAt)));
    }
}
