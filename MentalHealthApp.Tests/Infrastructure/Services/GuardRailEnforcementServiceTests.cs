using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Infrastructure.Services;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class GuardRailEnforcementServiceTests
{
    private GuardRailEnforcementService _service;

    [SetUp]
    public void Setup()
    {
        _service = new GuardRailEnforcementService();
    }

    private static GuardRail ActiveRemove(string keyword) => new GuardRail
    {
        Keyword = keyword,
        Action = GuardRailAction.Remove,
        IsActive = true
    };

    private static GuardRail ActiveReplace(string keyword, string? replacement = null) => new GuardRail
    {
        Keyword = keyword,
        Action = GuardRailAction.Replace,
        Replacement = replacement,
        IsActive = true
    };

    private static GuardRail Inactive(string keyword) => new GuardRail
    {
        Keyword = keyword,
        Action = GuardRailAction.Remove,
        IsActive = false
    };

    // ─── Remove ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ApplyGuardRails_Remove_KeywordIsRemovedFromContent()
    {
        var result = await _service.ApplyGuardRailsAsync(
            "Do not take medication without advice",
            new List<GuardRail> { ActiveRemove("medication") });

        Assert.That(result, Does.Not.Contain("medication"));
        Assert.That(result.Trim(), Is.EqualTo("Do not take  without advice"));
    }

    [Test]
    public async Task ApplyGuardRails_Remove_CaseInsensitive()
    {
        var result = await _service.ApplyGuardRailsAsync(
            "Take Medication and medication and MEDICATION",
            new List<GuardRail> { ActiveRemove("medication") });

        Assert.That(result, Does.Not.Contain("Medication"));
        Assert.That(result, Does.Not.Contain("medication"));
        Assert.That(result, Does.Not.Contain("MEDICATION"));
    }

    [Test]
    public async Task ApplyGuardRails_Remove_WordBoundaryPreserved()
    {
        var result = await _service.ApplyGuardRailsAsync(
            "medications are not the same as medication",
            new List<GuardRail> { ActiveRemove("medication") });

        Assert.That(result, Does.Contain("medications"));
        Assert.That(result, Does.Not.Contain(" medication "));
    }

    // ─── Replace ────────────────────────────────────────────────────────────

    [Test]
    public async Task ApplyGuardRails_Replace_KeywordIsSubstituted()
    {
        var result = await _service.ApplyGuardRailsAsync(
            "You should see a psychiatrist",
            new List<GuardRail> { ActiveReplace("psychiatrist", "professional") });

        Assert.That(result, Is.EqualTo("You should see a professional"));
    }

    [Test]
    public async Task ApplyGuardRails_Replace_NullReplacement_UsesRedacted()
    {
        var result = await _service.ApplyGuardRailsAsync(
            "Take Xanax daily",
            new List<GuardRail> { ActiveReplace("Xanax", null) });

        Assert.That(result, Is.EqualTo("Take [redacted] daily"));
    }

    [Test]
    public async Task ApplyGuardRails_Replace_CaseInsensitive()
    {
        var result = await _service.ApplyGuardRailsAsync(
            "ALCOHOL and alcohol and Alcohol",
            new List<GuardRail> { ActiveReplace("alcohol", "substances") });

        Assert.That(result, Does.Not.Contain("alcohol").IgnoreCase);
        Assert.That(result.Split("substances").Length - 1, Is.EqualTo(3));
    }

    // ─── Inactive guard rail ─────────────────────────────────────────────────

    [Test]
    public async Task ApplyGuardRails_InactiveGuardRail_IsSkipped()
    {
        var result = await _service.ApplyGuardRailsAsync(
            "Take medication daily",
            new List<GuardRail> { Inactive("medication") });

        Assert.That(result, Is.EqualTo("Take medication daily"));
    }

    // ─── Multiple guard rails ────────────────────────────────────────────────

    [Test]
    public async Task ApplyGuardRails_MultipleRules_AllApplied()
    {
        var guardRails = new List<GuardRail>
        {
            ActiveRemove("alcohol"),
            ActiveReplace("psychiatrist", "therapist")
        };

        var result = await _service.ApplyGuardRailsAsync(
            "Discuss alcohol with a psychiatrist",
            guardRails);

        Assert.That(result, Does.Not.Contain("alcohol"));
        Assert.That(result, Does.Contain("therapist"));
        Assert.That(result, Does.Not.Contain("psychiatrist"));
    }

    [Test]
    public async Task ApplyGuardRails_MixActiveAndInactive_OnlyActiveApplied()
    {
        var guardRails = new List<GuardRail>
        {
            Inactive("safe"),
            ActiveReplace("danger", "concern")
        };

        var result = await _service.ApplyGuardRailsAsync(
            "This is safe but the danger is real",
            guardRails);

        Assert.That(result, Does.Contain("safe"));
        Assert.That(result, Does.Not.Contain("danger"));
        Assert.That(result, Does.Contain("concern"));
    }

    // ─── Empty inputs ────────────────────────────────────────────────────────

    [Test]
    public async Task ApplyGuardRails_EmptyGuardRailList_ReturnsOriginalContent()
    {
        const string content = "Some mental health content";
        var result = await _service.ApplyGuardRailsAsync(content, new List<GuardRail>());

        Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public async Task ApplyGuardRails_EmptyContent_ReturnsEmpty()
    {
        var result = await _service.ApplyGuardRailsAsync(
            string.Empty,
            new List<GuardRail> { ActiveRemove("anything") });

        Assert.That(result, Is.EqualTo(string.Empty));
    }
}
