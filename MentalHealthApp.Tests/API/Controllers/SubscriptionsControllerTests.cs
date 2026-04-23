using MentalHealthApp.API.Controllers;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Stripe;
using System.Security.Claims;
using System.Text;

namespace MentalHealthApp.Tests.API.Controllers;

[TestFixture]
public class SubscriptionsControllerTests
{
    private Mock<ISubscriptionService> _subscriptionServiceMock;
    private Mock<IStripeWebhookParser> _webhookParserMock;
    private Mock<IConfiguration> _configurationMock;
    private SubscriptionsController _controller;

    [SetUp]
    public void Setup()
    {
        _subscriptionServiceMock = new Mock<ISubscriptionService>();
        _webhookParserMock = new Mock<IStripeWebhookParser>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(x => x["Stripe:WebhookSecret"]).Returns("whsec_test");

        _controller = new SubscriptionsController(
            _subscriptionServiceMock.Object,
            _webhookParserMock.Object,
            _configurationMock.Object);

        SetUser("patient123");
    }

    private void SetUser(string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Patient")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private void SetRequestBody(string json)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(json);
        _controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(bodyBytes);
        _controller.ControllerContext.HttpContext.Request.Headers["Stripe-Signature"] = "test-sig";
    }

    // ─── GetStatus ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetStatus_ValidRequest_ReturnsOkWithStatus()
    {
        var expected = new SubscriptionStatusResponse { Status = "Active", IsActive = true };
        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionStatusAsync("patient123", default))
            .ReturnsAsync(expected);

        var result = await _controller.GetStatus();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetStatus_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionStatusAsync("patient123", default))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.GetStatus();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetStatus_ServiceException_ReturnsBadRequest()
    {
        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionStatusAsync("patient123", default))
            .ThrowsAsync(new Exception("Something failed"));

        var result = await _controller.GetStatus();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetStatus_MissingUserClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.GetStatus();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    // ─── CreateCheckoutSession ───────────────────────────────────────────────

    [Test]
    public async Task CreateCheckoutSession_ValidRequest_ReturnsOkWithUrl()
    {
        var expected = new CreateCheckoutSessionResponse { CheckoutUrl = "https://checkout.stripe.com/pay/cs_123" };
        _subscriptionServiceMock
            .Setup(x => x.CreateCheckoutSessionAsync("patient123", default))
            .ReturnsAsync(expected);

        var result = await _controller.CreateCheckoutSession();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task CreateCheckoutSession_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        _subscriptionServiceMock
            .Setup(x => x.CreateCheckoutSessionAsync("patient123", default))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.CreateCheckoutSession();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task CreateCheckoutSession_ServiceException_ReturnsBadRequest()
    {
        _subscriptionServiceMock
            .Setup(x => x.CreateCheckoutSessionAsync("patient123", default))
            .ThrowsAsync(new InvalidOperationException("Patient not found"));

        var result = await _controller.CreateCheckoutSession();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateCheckoutSession_MissingUserClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.CreateCheckoutSession();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    // ─── Cancel ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Cancel_ValidRequest_ReturnsOkWithMessage()
    {
        _subscriptionServiceMock
            .Setup(x => x.CancelSubscriptionAsync("patient123", default))
            .Returns(Task.CompletedTask);

        var result = await _controller.Cancel();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Cancel_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        _subscriptionServiceMock
            .Setup(x => x.CancelSubscriptionAsync("patient123", default))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.Cancel();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Cancel_ServiceException_ReturnsBadRequest()
    {
        _subscriptionServiceMock
            .Setup(x => x.CancelSubscriptionAsync("patient123", default))
            .ThrowsAsync(new InvalidOperationException("No active subscription found"));

        var result = await _controller.Cancel();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Cancel_MissingUserClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.Cancel();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    // ─── Webhook ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Webhook_InvalidSignature_ReturnsBadRequest()
    {
        SetRequestBody("{}");
        _webhookParserMock
            .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new StripeException("Invalid signature"));

        var result = await _controller.Webhook();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Webhook_CheckoutSessionCompleted_CallsHandleCheckoutCompleted()
    {
        SetRequestBody("{}");
        var webhookEvent = new WebhookEvent
        {
            Type = "checkout.session.completed",
            PatientUserId = "patient123",
            CheckoutSessionId = "cs_abc",
            StripeSubscriptionId = "sub_abc"
        };
        _webhookParserMock
            .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);
        _subscriptionServiceMock
            .Setup(x => x.HandleCheckoutCompletedAsync("patient123", "cs_abc", "sub_abc", default))
            .Returns(Task.CompletedTask);

        var result = await _controller.Webhook();

        Assert.That(result, Is.InstanceOf<OkResult>());
        _subscriptionServiceMock.Verify(
            x => x.HandleCheckoutCompletedAsync("patient123", "cs_abc", "sub_abc", default),
            Times.Once);
    }

    [Test]
    public async Task Webhook_CheckoutSessionCompleted_MissingPatientId_SkipsHandler()
    {
        SetRequestBody("{}");
        var webhookEvent = new WebhookEvent
        {
            Type = "checkout.session.completed",
            PatientUserId = null,       // missing — should not call handler
            CheckoutSessionId = "cs_abc",
            StripeSubscriptionId = "sub_abc"
        };
        _webhookParserMock
            .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);

        var result = await _controller.Webhook();

        Assert.That(result, Is.InstanceOf<OkResult>());
        _subscriptionServiceMock.Verify(
            x => x.HandleCheckoutCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Test]
    public async Task Webhook_SubscriptionDeleted_CallsHandleSubscriptionCanceled()
    {
        SetRequestBody("{}");
        var webhookEvent = new WebhookEvent
        {
            Type = "customer.subscription.deleted",
            StripeSubscriptionId = "sub_abc"
        };
        _webhookParserMock
            .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);
        _subscriptionServiceMock
            .Setup(x => x.HandleSubscriptionCanceledAsync("sub_abc", default))
            .Returns(Task.CompletedTask);

        var result = await _controller.Webhook();

        Assert.That(result, Is.InstanceOf<OkResult>());
        _subscriptionServiceMock.Verify(
            x => x.HandleSubscriptionCanceledAsync("sub_abc", default),
            Times.Once);
    }

    [Test]
    public async Task Webhook_InvoicePaymentSucceeded_CallsHandlePaymentSucceeded()
    {
        SetRequestBody("{}");
        var periodEnd = DateTime.UtcNow.AddMonths(1);
        var webhookEvent = new WebhookEvent
        {
            Type = "invoice.payment_succeeded",
            StripeSubscriptionId = "sub_abc",
            PeriodEnd = periodEnd
        };
        _webhookParserMock
            .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);
        _subscriptionServiceMock
            .Setup(x => x.HandlePaymentSucceededAsync("sub_abc", periodEnd, default))
            .Returns(Task.CompletedTask);

        var result = await _controller.Webhook();

        Assert.That(result, Is.InstanceOf<OkResult>());
        _subscriptionServiceMock.Verify(
            x => x.HandlePaymentSucceededAsync("sub_abc", periodEnd, default),
            Times.Once);
    }

    [Test]
    public async Task Webhook_InvoicePaymentSucceeded_NullPeriodEnd_UsesDefaultFallback()
    {
        SetRequestBody("{}");
        var webhookEvent = new WebhookEvent
        {
            Type = "invoice.payment_succeeded",
            StripeSubscriptionId = "sub_abc",
            PeriodEnd = null
        };
        _webhookParserMock
            .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);
        _subscriptionServiceMock
            .Setup(x => x.HandlePaymentSucceededAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), default))
            .Returns(Task.CompletedTask);

        var result = await _controller.Webhook();

        Assert.That(result, Is.InstanceOf<OkResult>());
        _subscriptionServiceMock.Verify(
            x => x.HandlePaymentSucceededAsync("sub_abc", It.IsAny<DateTime>(), default),
            Times.Once);
    }

    [Test]
    public async Task Webhook_UnknownEventType_ReturnsOkWithoutCallingService()
    {
        SetRequestBody("{}");
        var webhookEvent = new WebhookEvent { Type = "some.unknown.event" };
        _webhookParserMock
            .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);

        var result = await _controller.Webhook();

        Assert.That(result, Is.InstanceOf<OkResult>());
        _subscriptionServiceMock.Verify(
            x => x.HandleCheckoutCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
        _subscriptionServiceMock.Verify(
            x => x.HandleSubscriptionCanceledAsync(It.IsAny<string>(), default),
            Times.Never);
        _subscriptionServiceMock.Verify(
            x => x.HandlePaymentSucceededAsync(It.IsAny<string>(), It.IsAny<DateTime>(), default),
            Times.Never);
    }
}
