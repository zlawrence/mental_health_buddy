using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class EmergencyContact
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("patientUserId")]
    public string PatientUserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("smsNumber")]
    public string? SmsNumber { get; set; }

    [BsonElement("isPrimary")]
    public bool IsPrimary { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
