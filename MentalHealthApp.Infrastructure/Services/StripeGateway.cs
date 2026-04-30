using MentalHealthApp.Application.Services;
using MentalHealthApp.fx.Classes.Abstract;
using MentalHealthApp.Infrastructure.Resilience;
using Polly;
using Stripe;
using Stripe.Checkout;

namespace MentalHealthApp.Infrastructure.Services;

public class StripeGateway : PaymentGateway, IPaymentGateway
{
    private readonly string _secretKey;
    private readonly ResiliencePipeline _pipeline;

    public StripeGateway(string secretKey, string gatewayEnvironment, ApiResiliencePipelineProvider pipelineProvider)
        : base(gatewayEnvironment)
    {
        _secretKey = secretKey;
        _pipeline = pipelineProvider.GetPipeline("Stripe");
    }

    public async Task<string> CreateCustomerAsync(string email, string patientUserId, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _secretKey;

        if (IsDevelopment)
            return new Guid().ToString();

        var options = new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string> { { "patientUserId", patientUserId } }
        };

        return await _pipeline.ExecuteAsync(async ct =>
        {
            var service = new CustomerService();
            var customer = await service.CreateAsync(options, cancellationToken: ct);
            return customer.Id;
        }, cancellationToken);
    }

    public async Task<(string SessionId, string SessionUrl)> CreateCheckoutSessionAsync(
        string customerId, string successUrl, string cancelUrl, string patientUserId, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _secretKey;

        if (IsDevelopment)
        {
            string fakeId = new Guid().ToString();
            return (fakeId, $"localhost/{fakeId}");
        }

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = 500,
                        Recurring = new SessionLineItemPriceDataRecurringOptions { Interval = "month" },
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "Anxiety Buddy Subscription" }
                    },
                    Quantity = 1
                }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string> { { "patientUserId", patientUserId } }
        };

        return await _pipeline.ExecuteAsync(async ct =>
        {
            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);
            return (session.Id, session.Url);
        }, cancellationToken);
    }

    public async Task CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _secretKey;

        await _pipeline.ExecuteAsync(async ct =>
        {
            var service = new Stripe.SubscriptionService();
            await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            }, cancellationToken: ct);
        }, cancellationToken);
    }
}
