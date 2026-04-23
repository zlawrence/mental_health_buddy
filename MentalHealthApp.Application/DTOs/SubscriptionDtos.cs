namespace MentalHealthApp.Application.DTOs;

public record WebhookEvent
{
    public required string Type { get; init; }
    public string? PatientUserId { get; init; }
    public string? CheckoutSessionId { get; init; }
    public string? StripeSubscriptionId { get; init; }
    public DateTime? PeriodEnd { get; init; }
}


public class CreateCheckoutSessionResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
}

public class SubscriptionStatusResponse
{
    public string Status { get; set; } = "Inactive";
    public bool IsActive { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? RenewalDate { get; set; }
    public DateTime? CanceledAt { get; set; }
}
