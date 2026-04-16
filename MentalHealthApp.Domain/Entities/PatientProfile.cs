using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class PatientProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("patientUserId")]
    public string PatientUserId { get; set; } = string.Empty;

    [BsonElement("therapistUserIds")]
    public List<string> TherapistUserIds { get; set; } = new();

    [BsonElement("emergencyContactIds")]
    public List<string> EmergencyContactIds { get; set; } = new();

    [BsonElement("conversationRetentionDays")]
    public int ConversationRetentionDays { get; set; } = -1; // -1 = forever

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
