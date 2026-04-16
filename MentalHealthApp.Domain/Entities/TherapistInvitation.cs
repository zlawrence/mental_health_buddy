using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class TherapistInvitation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("patientUserId")]
    public string PatientUserId { get; set; } = string.Empty;

    [BsonElement("therapistEmail")]
    public string TherapistEmail { get; set; } = string.Empty;

    [BsonElement("token")]
    public string Token { get; set; } = string.Empty; // JWT token

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } // 7 days from creation

    [BsonElement("isUsed")]
    public bool IsUsed { get; set; } = false;

    [BsonElement("usedBy")]
    public string? UsedBy { get; set; } // TherapistId if accepted

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
