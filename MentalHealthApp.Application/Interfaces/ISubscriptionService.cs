using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface ISubscriptionService
{
    Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(string patientUserId, CancellationToken cancellationToken = default);
    Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(string patientUserId, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAsync(string patientUserId, CancellationToken cancellationToken = default);
    Task HandleCheckoutCompletedAsync(string patientUserId, string checkoutSessionId, string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task HandleSubscriptionCanceledAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task HandlePaymentSucceededAsync(string stripeSubscriptionId, DateTime periodEnd, CancellationToken cancellationToken = default);
}
