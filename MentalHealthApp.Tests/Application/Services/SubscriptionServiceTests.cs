using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Application.Services;

[TestFixture]
public class SubscriptionServiceTests
{
    private Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IPaymentGateway> _stripeGatewayMock;
    private SubscriptionService _subscriptionService;

    private const string SuccessUrl = "https://example.com/success";
    private const string CancelUrl = "https://example.com/cancel";

    [SetUp]
    public void Setup()
    {
        _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _stripeGatewayMock = new Mock<IPaymentGateway>();

        _subscriptionService = new SubscriptionService(
            _subscriptionRepositoryMock.Object,
            _userRepositoryMock.Object,
            _stripeGatewayMock.Object,
            SuccessUrl,
            CancelUrl);
    }

    // ─── GetSubscriptionStatusAsync ─────────────────────────────────────────

    [Test]
    public async Task GetSubscriptionStatusAsync_NoSubscription_ReturnsInactive()
    {
        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync("patient1", default))
            .ReturnsAsync((Subscription?)null);

        var result = await _subscriptionService.GetSubscriptionStatusAsync("patient1");

        Assert.That(result.Status, Is.EqualTo("Inactive"));
        Assert.That(result.IsActive, Is.False);
        Assert.That(result.PurchaseDate, Is.Null);
        Assert.That(result.RenewalDate, Is.Null);
    }

    [Test]
    public async Task GetSubscriptionStatusAsync_ActiveSubscription_ReturnsActiveStatus()
    {
        var renewalDate = DateTime.UtcNow.AddMonths(1);
        var purchaseDate = DateTime.UtcNow.AddDays(-15);
        var subscription = new Subscription
        {
            PatientUserId = "patient1",
            Status = SubscriptionStatus.Active,
            PurchaseDate = purchaseDate,
            RenewalDate = renewalDate
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync("patient1", default))
            .ReturnsAsync(subscription);

        var result = await _subscriptionService.GetSubscriptionStatusAsync("patient1");

        Assert.That(result.Status, Is.EqualTo("Active"));
        Assert.That(result.IsActive, Is.True);
        Assert.That(result.PurchaseDate, Is.EqualTo(purchaseDate));
        Assert.That(result.RenewalDate, Is.EqualTo(renewalDate));
    }

    [Test]
    public async Task GetSubscriptionStatusAsync_CanceledSubscription_ReturnsCanceledStatus()
    {
        var canceledAt = DateTime.UtcNow.AddDays(-1);
        var subscription = new Subscription
        {
            PatientUserId = "patient1",
            Status = SubscriptionStatus.Canceled,
            CanceledAt = canceledAt
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync("patient1", default))
            .ReturnsAsync(subscription);

        var result = await _subscriptionService.GetSubscriptionStatusAsync("patient1");

        Assert.That(result.Status, Is.EqualTo("Canceled"));
        Assert.That(result.IsActive, Is.False);
        Assert.That(result.CanceledAt, Is.EqualTo(canceledAt));
    }

    [Test]
    public async Task GetSubscriptionStatusAsync_PendingSubscription_ReturnsCorrectStatus()
    {
        var subscription = new Subscription
        {
            PatientUserId = "patient1",
            Status = SubscriptionStatus.Pending
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync("patient1", default))
            .ReturnsAsync(subscription);

        var result = await _subscriptionService.GetSubscriptionStatusAsync("patient1");

        Assert.That(result.Status, Is.EqualTo("Pending"));
        Assert.That(result.IsActive, Is.False);
    }

    // ─── CreateCheckoutSessionAsync ──────────────────────────────────────────

    [Test]
    public async Task CreateCheckoutSessionAsync_NoExistingSubscription_CreatesCustomerAndSession()
    {
        const string patientId = "patient1";
        const string customerId = "cus_abc123";
        const string sessionId = "cs_abc123";
        const string sessionUrl = "https://checkout.stripe.com/pay/cs_abc123";

        var user = new User { Id = patientId, Email = "patient@example.com" };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync(patientId, default))
            .ReturnsAsync((Subscription?)null);
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(patientId, default))
            .ReturnsAsync(user);
        _stripeGatewayMock
            .Setup(x => x.CreateCustomerAsync(user.Email, patientId, default))
            .ReturnsAsync(customerId);
        _stripeGatewayMock
            .Setup(x => x.CreateCheckoutSessionAsync(customerId, SuccessUrl, CancelUrl, patientId, default))
            .ReturnsAsync((sessionId, sessionUrl));
        _subscriptionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Subscription>(), default))
            .ReturnsAsync(new Subscription());

        var result = await _subscriptionService.CreateCheckoutSessionAsync(patientId);

        Assert.That(result.CheckoutUrl, Is.EqualTo(sessionUrl));
        _stripeGatewayMock.Verify(x => x.CreateCustomerAsync(user.Email, patientId, default), Times.Once);
        _subscriptionRepositoryMock.Verify(x => x.AddAsync(It.Is<Subscription>(s =>
            s.PatientUserId == patientId &&
            s.StripeCustomerId == customerId &&
            s.StripeCheckoutSessionId == sessionId &&
            s.Status == SubscriptionStatus.Pending), default), Times.Once);
    }

    [Test]
    public async Task CreateCheckoutSessionAsync_ExistingCustomer_SkipsCustomerCreation()
    {
        const string patientId = "patient1";
        const string existingCustomerId = "cus_existing";
        const string sessionId = "cs_new";
        const string sessionUrl = "https://checkout.stripe.com/pay/cs_new";

        var existingSubscription = new Subscription
        {
            PatientUserId = patientId,
            StripeCustomerId = existingCustomerId,
            Status = SubscriptionStatus.Pending
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync(patientId, default))
            .ReturnsAsync(existingSubscription);
        _stripeGatewayMock
            .Setup(x => x.CreateCheckoutSessionAsync(existingCustomerId, SuccessUrl, CancelUrl, patientId, default))
            .ReturnsAsync((sessionId, sessionUrl));
        _subscriptionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        var result = await _subscriptionService.CreateCheckoutSessionAsync(patientId);

        Assert.That(result.CheckoutUrl, Is.EqualTo(sessionUrl));
        _stripeGatewayMock.Verify(x => x.CreateCustomerAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Subscription>(s =>
            s.StripeCheckoutSessionId == sessionId &&
            s.Status == SubscriptionStatus.Pending), default), Times.Once);
    }

    [Test]
    public void CreateCheckoutSessionAsync_PatientNotFound_ThrowsInvalidOperation()
    {
        const string patientId = "nonexistent";

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync(patientId, default))
            .ReturnsAsync((Subscription?)null);
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(patientId, default))
            .ReturnsAsync((User?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _subscriptionService.CreateCheckoutSessionAsync(patientId));
        Assert.That(ex.Message, Is.EqualTo("Patient not found"));
    }

    // ─── CancelSubscriptionAsync ─────────────────────────────────────────────

    [Test]
    public async Task CancelSubscriptionAsync_ActiveSubscription_CancelsAtPeriodEnd()
    {
        const string patientId = "patient1";
        const string stripeSubId = "sub_abc123";

        var subscription = new Subscription
        {
            PatientUserId = patientId,
            StripeSubscriptionId = stripeSubId,
            Status = SubscriptionStatus.Active
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync(patientId, default))
            .ReturnsAsync(subscription);
        _stripeGatewayMock
            .Setup(x => x.CancelSubscriptionAtPeriodEndAsync(stripeSubId, default))
            .Returns(Task.CompletedTask);
        _subscriptionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        await _subscriptionService.CancelSubscriptionAsync(patientId);

        _stripeGatewayMock.Verify(x => x.CancelSubscriptionAtPeriodEndAsync(stripeSubId, default), Times.Once);
        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Subscription>(s =>
            s.Status == SubscriptionStatus.Canceled &&
            s.CanceledAt != null), default), Times.Once);
    }

    [Test]
    public void CancelSubscriptionAsync_NoSubscription_ThrowsInvalidOperation()
    {
        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync("patient1", default))
            .ReturnsAsync((Subscription?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _subscriptionService.CancelSubscriptionAsync("patient1"));
        Assert.That(ex.Message, Is.EqualTo("No active subscription found"));
    }

    [Test]
    public void CancelSubscriptionAsync_PendingSubscriptionWithNoStripeId_ThrowsInvalidOperation()
    {
        var subscription = new Subscription
        {
            PatientUserId = "patient1",
            StripeSubscriptionId = null,
            Status = SubscriptionStatus.Pending
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync("patient1", default))
            .ReturnsAsync(subscription);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _subscriptionService.CancelSubscriptionAsync("patient1"));
        Assert.That(ex.Message, Is.EqualTo("Subscription is not yet active"));
    }

    // ─── HandleCheckoutCompletedAsync ───────────────────────────────────────

    [Test]
    public async Task HandleCheckoutCompletedAsync_ValidSubscription_UpdatesStatusToActive()
    {
        const string patientId = "patient1";
        const string sessionId = "cs_abc";
        const string stripeSubId = "sub_abc";

        var subscription = new Subscription
        {
            PatientUserId = patientId,
            Status = SubscriptionStatus.Pending
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync(patientId, default))
            .ReturnsAsync(subscription);
        _subscriptionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        await _subscriptionService.HandleCheckoutCompletedAsync(patientId, sessionId, stripeSubId);

        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Subscription>(s =>
            s.Status == SubscriptionStatus.Active &&
            s.StripeSubscriptionId == stripeSubId &&
            s.PurchaseDate != null), default), Times.Once);
    }

    [Test]
    public async Task HandleCheckoutCompletedAsync_SubscriptionNotFound_CompletesWithoutError()
    {
        _subscriptionRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync("patient1", default))
            .ReturnsAsync((Subscription?)null);

        await _subscriptionService.HandleCheckoutCompletedAsync("patient1", "cs_abc", "sub_abc");

        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Subscription>(), default), Times.Never);
    }

    // ─── HandleSubscriptionCanceledAsync ────────────────────────────────────

    [Test]
    public async Task HandleSubscriptionCanceledAsync_ValidSubscription_UpdatesStatusToInactive()
    {
        const string stripeSubId = "sub_abc";
        var subscription = new Subscription
        {
            StripeSubscriptionId = stripeSubId,
            Status = SubscriptionStatus.Active
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByStripeSubscriptionIdAsync(stripeSubId, default))
            .ReturnsAsync(subscription);
        _subscriptionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        await _subscriptionService.HandleSubscriptionCanceledAsync(stripeSubId);

        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(
            It.Is<Subscription>(s => s.Status == SubscriptionStatus.Inactive), default), Times.Once);
    }

    [Test]
    public async Task HandleSubscriptionCanceledAsync_SubscriptionNotFound_CompletesWithoutError()
    {
        _subscriptionRepositoryMock
            .Setup(x => x.GetByStripeSubscriptionIdAsync("sub_notfound", default))
            .ReturnsAsync((Subscription?)null);

        await _subscriptionService.HandleSubscriptionCanceledAsync("sub_notfound");

        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Subscription>(), default), Times.Never);
    }

    // ─── HandlePaymentSucceededAsync ─────────────────────────────────────────

    [Test]
    public async Task HandlePaymentSucceededAsync_ValidSubscription_UpdatesRenewalDate()
    {
        const string stripeSubId = "sub_abc";
        var periodEnd = DateTime.UtcNow.AddMonths(1);
        var subscription = new Subscription
        {
            StripeSubscriptionId = stripeSubId,
            Status = SubscriptionStatus.Active
        };

        _subscriptionRepositoryMock
            .Setup(x => x.GetByStripeSubscriptionIdAsync(stripeSubId, default))
            .ReturnsAsync(subscription);
        _subscriptionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Subscription>(), default))
            .Returns(Task.CompletedTask);

        await _subscriptionService.HandlePaymentSucceededAsync(stripeSubId, periodEnd);

        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Subscription>(s =>
            s.Status == SubscriptionStatus.Active &&
            s.RenewalDate == periodEnd), default), Times.Once);
    }

    [Test]
    public async Task HandlePaymentSucceededAsync_SubscriptionNotFound_CompletesWithoutError()
    {
        _subscriptionRepositoryMock
            .Setup(x => x.GetByStripeSubscriptionIdAsync("sub_notfound", default))
            .ReturnsAsync((Subscription?)null);

        await _subscriptionService.HandlePaymentSucceededAsync("sub_notfound", DateTime.UtcNow);

        _subscriptionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Subscription>(), default), Times.Never);
    }
}
