namespace MentalHealthApp.Application.Services;


/// <summary>
/// This payment gateway needs to be platform agnostic, so that we can allow for multiple options
/// </summary>
public interface IPaymentGateway
{
    bool IsDevelopment { get; }
    Task<string> CreateCustomerAsync(string email, string patientUserId, CancellationToken cancellationToken = default);
    Task<(string SessionId, string SessionUrl)> CreateCheckoutSessionAsync(string customerId, string successUrl, string cancelUrl, string patientUserId, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
}
