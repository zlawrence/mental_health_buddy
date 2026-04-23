using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Stripe;
using Stripe.Checkout;

namespace MentalHealthApp.Infrastructure.Services;

public class StripeWebhookParser : IStripeWebhookParser
{
    public WebhookEvent Parse(string json, string stripeSignature, string webhookSecret)
    {
        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

        return stripeEvent.Type switch
        {
            "checkout.session.completed" when stripeEvent.Data.Object is Session session
                => new WebhookEvent
                {
                    Type = stripeEvent.Type,
                    PatientUserId = session.Metadata.GetValueOrDefault("patientUserId"),
                    CheckoutSessionId = session.Id,
                    StripeSubscriptionId = session.SubscriptionId
                },

            "customer.subscription.deleted" when stripeEvent.Data.Object is Stripe.Subscription sub
                => new WebhookEvent
                {
                    Type = stripeEvent.Type,
                    StripeSubscriptionId = sub.Id
                },

            "invoice.payment_succeeded" when stripeEvent.Data.Object is Invoice invoice
                => new WebhookEvent
                {
                    Type = stripeEvent.Type,
                    StripeSubscriptionId = invoice.SubscriptionId,
                    PeriodEnd = invoice.Lines.Data.FirstOrDefault()?.Period?.End
                },

            _ => new WebhookEvent { Type = stripeEvent.Type }
        };
    }
}
