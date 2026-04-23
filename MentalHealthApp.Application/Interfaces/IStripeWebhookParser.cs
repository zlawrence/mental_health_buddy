using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IStripeWebhookParser
{
    WebhookEvent Parse(string json, string stripeSignature, string webhookSecret);
}
