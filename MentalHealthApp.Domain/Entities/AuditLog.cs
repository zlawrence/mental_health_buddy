using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("adminId")]
    public string AdminId { get; set; } = string.Empty;

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // e.g., "ResetPassword", "DeactivatePatient"

    [BsonElement("targetId")]
    public string? TargetId { get; set; }

    [BsonElement("targetType")]
    public string? TargetType { get; set; } // e.g., "Patient", "Therapist", "Admin"

    [BsonElement("details")]
    public string? Details { get; set; } // JSON or description

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
