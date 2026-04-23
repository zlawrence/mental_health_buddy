using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class RateLimitingServiceTests
{
    private Mock<IMessageCountRepository> _messageCountRepositoryMock;
    private RateLimitingService _service;

    [SetUp]
    public void Setup()
    {
        _messageCountRepositoryMock = new Mock<IMessageCountRepository>();
        _service = new RateLimitingService(_messageCountRepositoryMock.Object);
    }

    private void SetupCount(string patientId, int count)
    {
        _messageCountRepositoryMock
            .Setup(x => x.IncrementCountAsync(patientId, It.IsAny<DateTime>(), default))
            .ReturnsAsync(new MessageCount { PatientUserId = patientId, Count = count });
    }

    // ─── AllowedToChat ───────────────────────────────────────────────────────

    [Test]
    public async Task CheckAndIncrement_UnderLimit_AllowedToChat()
    {
        SetupCount("patient1", 5);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.AllowedToChat, Is.True);
    }

    [Test]
    public async Task CheckAndIncrement_UnderLimit_MessageIsOk()
    {
        SetupCount("patient1", 10);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.Message, Is.EqualTo("OK"));
    }

    [Test]
    public async Task CheckAndIncrement_UnderLimit_CountAndMaxReturned()
    {
        SetupCount("patient1", 5);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.CurrentCount, Is.EqualTo(5));
        Assert.That(result.MaxAllowed, Is.EqualTo(20));
    }

    // ─── Warning threshold (18-20) ───────────────────────────────────────────

    [Test]
    public async Task CheckAndIncrement_AtWarningThreshold_AllowedToChat()
    {
        SetupCount("patient1", 18);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.AllowedToChat, Is.True);
    }

    [Test]
    public async Task CheckAndIncrement_AtWarningThreshold_MessageContainsWarning()
    {
        SetupCount("patient1", 18);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.Message, Does.Contain("approaching").IgnoreCase
            .Or.Contains("limit").IgnoreCase);
    }

    [Test]
    public async Task CheckAndIncrement_At19Messages_StillAllowed()
    {
        SetupCount("patient1", 19);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.AllowedToChat, Is.True);
    }

    [Test]
    public async Task CheckAndIncrement_At20Messages_StillAllowed()
    {
        SetupCount("patient1", 20);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.AllowedToChat, Is.True);
    }

    [Test]
    public async Task CheckAndIncrement_WarningThreshold_MessageContainsCount()
    {
        SetupCount("patient1", 18);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.Message, Does.Contain("18").Or.Contains("20"));
    }

    // ─── Over limit (> 20) ──────────────────────────────────────────────────

    [Test]
    public async Task CheckAndIncrement_OverLimit_NotAllowedToChat()
    {
        SetupCount("patient1", 21);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.AllowedToChat, Is.False);
    }

    [Test]
    public async Task CheckAndIncrement_OverLimit_MessageIndicatesLimit()
    {
        SetupCount("patient1", 21);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.Message, Does.Contain("limit").IgnoreCase
            .Or.Contains("tomorrow").IgnoreCase);
    }

    [Test]
    public async Task CheckAndIncrement_OverLimit_CountAndMaxReturned()
    {
        SetupCount("patient1", 25);

        var result = await _service.CheckAndIncrementAsync("patient1");

        Assert.That(result.CurrentCount, Is.EqualTo(25));
        Assert.That(result.MaxAllowed, Is.EqualTo(20));
    }

    // ─── Repository interaction ──────────────────────────────────────────────

    [Test]
    public async Task CheckAndIncrement_CallsIncrementWithTodaysDate()
    {
        SetupCount("patient1", 1);
        var today = DateTime.UtcNow.Date;

        await _service.CheckAndIncrementAsync("patient1");

        _messageCountRepositoryMock.Verify(
            x => x.IncrementCountAsync("patient1", It.Is<DateTime>(d => d.Date == today), default),
            Times.Once);
    }
}
