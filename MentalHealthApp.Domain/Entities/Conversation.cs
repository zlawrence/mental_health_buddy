using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class Conversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("patientUserId")]
    public string PatientUserId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("startedDate")]
    public DateTime StartedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("isArchived")]
    public bool IsArchived { get; set; } = false;

    [BsonElement("messageCount")]
    public int MessageCount { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
