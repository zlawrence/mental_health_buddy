using MentalHealthApp.Application.Services;
using Stripe;
using Stripe.Checkout;
using MentalHealthApp.fx.Classes.Abstract;

namespace MentalHealthApp.Infrastructure.Services;

public class StripeGateway : PaymentGateway, IPaymentGateway
{
    private readonly string _secretKey;
    private readonly string _environment = "dev";

    public StripeGateway(string secretKey, string gatewayEnvironment) : base(gatewayEnvironment)
    {
        _secretKey = secretKey;
    }

    public async Task<string> CreateCustomerAsync(string email, string patientUserId, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _secretKey;
        var options = new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string> { { "patientUserId", patientUserId } }
        };

        // If we're using development mode, don't contact the stripe service
        if (!this.IsDevelopment) { 
            var service = new CustomerService();
            var customer = await service.CreateAsync(options, cancellationToken: cancellationToken);
            return customer.Id;
        }
        else
        {
            // Return a fake id
            return new Guid().ToString();
        }
    }

    public async Task<(string SessionId, string SessionUrl)> CreateCheckoutSessionAsync(
        string customerId, string successUrl, string cancelUrl, string patientUserId, CancellationToken cancellationToken = default)
    {

        StripeConfiguration.ApiKey = _secretKey;
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

        if (this.IsDevelopment){
            // Return a fake session id and url
            string fakeId = new Guid().ToString();
            return (fakeId, "localhost${fakeId}");

        }
        else {
            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
            return (session.Id, session.Url);
        }
    }

    public async Task CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _secretKey;
        var service = new Stripe.SubscriptionService();
        await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        }, cancellationToken: cancellationToken);
    }
}
