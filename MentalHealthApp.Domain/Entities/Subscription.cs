using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MentalHealthApp.Domain.Entities;

public class Subscription
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("patientUserId")]
    public string PatientUserId { get; set; } = string.Empty;

    [BsonElement("stripeCustomerId")]
    public string StripeCustomerId { get; set; } = string.Empty;

    [BsonElement("stripeSubscriptionId")]
    public string? StripeSubscriptionId { get; set; }

    [BsonElement("stripeCheckoutSessionId")]
    public string? StripeCheckoutSessionId { get; set; }

    [BsonElement("status")]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Inactive;

    [BsonElement("purchaseDate")]
    public DateTime? PurchaseDate { get; set; }

    [BsonElement("renewalDate")]
    public DateTime? RenewalDate { get; set; }

    [BsonElement("canceledAt")]
    public DateTime? CanceledAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum SubscriptionStatus
{
    Inactive,
    Pending,
    Active,
    Canceled,
    PastDue
}
