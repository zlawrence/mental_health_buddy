using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class MessageLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("messageType")]
    public MessageType MessageType { get; set; }

    [BsonElement("to")]
    public string To { get; set; } = string.Empty;

    [BsonElement("subject")]
    public string? Subject { get; set; }

    [BsonElement("htmlBody")]
    public string? HtmlBody { get; set; }

    [BsonElement("textBody")]
    public string TextBody { get; set; } = string.Empty;

    [BsonElement("status")]
    public MessageLogStatus Status { get; set; } = MessageLogStatus.Queued;

    [BsonElement("failureMessage")]
    public string? FailureMessage { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("sentAt")]
    public DateTime? SentAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum MessageType
{
    Email,
    Sms
}

public enum MessageLogStatus
{
    Queued,
    Sent,
    Failed
}
