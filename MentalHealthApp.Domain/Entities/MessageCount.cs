using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class MessageCount
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("patientUserId")]
    public string PatientUserId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; } // Date in UTC (no time component)

    [BsonElement("count")]
    public int Count { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
