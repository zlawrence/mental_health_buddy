using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly string _successUrl;
    private readonly string _cancelUrl;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        IPaymentGateway paymentGateway,
        string successUrl,
        string cancelUrl )
    {
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
        _paymentGateway = paymentGateway;
        _successUrl = successUrl;
        _cancelUrl = cancelUrl;
    }

    public async Task<SubscriptionStatusResponse> GetSubscriptionStatusAsync(string patientUserId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByPatientUserIdAsync(patientUserId, cancellationToken);

        if (subscription == null)
            return new SubscriptionStatusResponse { Status = "Inactive", IsActive = false };

        return new SubscriptionStatusResponse
        {
            Status = subscription.Status.ToString(),
            IsActive = subscription.Status == SubscriptionStatus.Active,
            PurchaseDate = subscription.PurchaseDate,
            RenewalDate = subscription.RenewalDate,
            CanceledAt = subscription.CanceledAt
        };
    }

    public async Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(string patientUserId, CancellationToken cancellationToken = default)
    {
        var existing = await _subscriptionRepository.GetByPatientUserIdAsync(patientUserId, cancellationToken);

        string customerId;
        if (!string.IsNullOrEmpty(existing?.StripeCustomerId))
        {
            customerId = existing.StripeCustomerId;
        }
        else
        {
            var user = await _userRepository.GetByIdAsync(patientUserId, cancellationToken)
                ?? throw new InvalidOperationException("Patient not found");
            customerId = await _paymentGateway.CreateCustomerAsync(user.Email, patientUserId, cancellationToken);
        }

        var (sessionId, sessionUrl) = await _paymentGateway.CreateCheckoutSessionAsync(
            customerId, _successUrl, _cancelUrl, patientUserId, cancellationToken);

        SubscriptionStatus subscriptionStatus = SubscriptionStatus.Pending;

        if (_paymentGateway.IsDevelopment)
        {
            subscriptionStatus = SubscriptionStatus.Active;
        }

        if (existing == null)
        {
            var subscription = new Subscription
            {
                PatientUserId = patientUserId,
                StripeCustomerId = customerId,
                StripeCheckoutSessionId = sessionId,
                Status = subscriptionStatus
            };
            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        }
        else
        {
            existing.StripeCustomerId = customerId;
            existing.StripeCheckoutSessionId = sessionId;
            existing.Status = subscriptionStatus;
            existing.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(existing, cancellationToken);
        }

        return new CreateCheckoutSessionResponse { CheckoutUrl = sessionUrl };
    }

    public async Task CancelSubscriptionAsync(string patientUserId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByPatientUserIdAsync(patientUserId, cancellationToken)
            ?? throw new InvalidOperationException("No active subscription found");

        if (subscription.StripeSubscriptionId == null)
            throw new InvalidOperationException("Subscription is not yet active");

        await _paymentGateway.CancelSubscriptionAtPeriodEndAsync(subscription.StripeSubscriptionId, cancellationToken);

        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    }

    public async Task HandleCheckoutCompletedAsync(string patientUserId, string checkoutSessionId, string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByPatientUserIdAsync(patientUserId, cancellationToken);
        if (subscription == null) return;

        subscription.StripeSubscriptionId = stripeSubscriptionId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.PurchaseDate = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    }

    public async Task HandleSubscriptionCanceledAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId, cancellationToken);
        if (subscription == null) return;

        subscription.Status = SubscriptionStatus.Inactive;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    }

    public async Task HandlePaymentSucceededAsync(string stripeSubscriptionId, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId, cancellationToken);
        if (subscription == null) return;

        subscription.Status = SubscriptionStatus.Active;
        subscription.RenewalDate = periodEnd;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    }
}
