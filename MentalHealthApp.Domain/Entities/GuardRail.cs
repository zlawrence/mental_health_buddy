using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class GuardRail
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("patientUserId")]
    public string PatientUserId { get; set; } = string.Empty;

    [BsonElement("therapistUserId")]
    public string TherapistUserId { get; set; } = string.Empty;

    [BsonElement("keyword")]
    public string Keyword { get; set; } = string.Empty;

    [BsonElement("action")]
    public GuardRailAction Action { get; set; } = GuardRailAction.Remove;

    [BsonElement("replacement")]
    public string? Replacement { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum GuardRailAction
{
    Remove,
    Replace
}
