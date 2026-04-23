using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace MentalHealthApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IStripeWebhookParser _webhookParser;
    private readonly string _webhookSecret;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        IStripeWebhookParser webhookParser,
        IConfiguration configuration)
    {
        _subscriptionService = subscriptionService;
        _webhookParser = webhookParser;
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
    }

    [HttpGet("status")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var patientUserId = GetCurrentUserId();
            var result = await _subscriptionService.GetSubscriptionStatusAsync(patientUserId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("create-checkout-session")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateCheckoutSession()
    {
        try
        {
            var patientUserId = GetCurrentUserId();
            var result = await _subscriptionService.CreateCheckoutSessionAsync(patientUserId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cancel")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Cancel()
    {
        try
        {
            var patientUserId = GetCurrentUserId();
            await _subscriptionService.CancelSubscriptionAsync(patientUserId);
            return Ok(new { message = "Subscription will be canceled at the end of the current billing period" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhookEvent = _webhookParser.Parse(
                json,
                Request.Headers["Stripe-Signature"].ToString(),
                _webhookSecret);

            switch (webhookEvent.Type)
            {
                case "checkout.session.completed":
                    if (webhookEvent.PatientUserId != null
                        && webhookEvent.CheckoutSessionId != null
                        && webhookEvent.StripeSubscriptionId != null)
                    {
                        await _subscriptionService.HandleCheckoutCompletedAsync(
                            webhookEvent.PatientUserId,
                            webhookEvent.CheckoutSessionId,
                            webhookEvent.StripeSubscriptionId);
                    }
                    break;

                case "customer.subscription.deleted":
                    if (webhookEvent.StripeSubscriptionId != null)
                        await _subscriptionService.HandleSubscriptionCanceledAsync(webhookEvent.StripeSubscriptionId);
                    break;

                case "invoice.payment_succeeded":
                    if (webhookEvent.StripeSubscriptionId != null)
                        await _subscriptionService.HandlePaymentSucceededAsync(
                            webhookEvent.StripeSubscriptionId,
                            webhookEvent.PeriodEnd ?? DateTime.UtcNow.AddMonths(1));
                    break;
            }

            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest(new { message = "Invalid Stripe webhook signature" });
        }
    }

    private string GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User identifier claim is missing.");
        return userId;
    }
}
