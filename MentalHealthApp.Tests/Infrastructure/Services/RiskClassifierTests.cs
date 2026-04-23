using MentalHealthApp.Fx;
using MentalHealthApp.Infrastructure.Services;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class RiskClassifierTests
{
    private RiskClassifier _classifier;

    [SetUp]
    public void Setup()
    {
        _classifier = new RiskClassifier();
    }

    // ─── Crisis ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ClassifyRisk_SuicideKeyword_ReturnsCrisis()
    {
        var result = await _classifier.ClassifyRiskAsync("I am thinking about suicide");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Crisis));
    }

    [Test]
    public async Task ClassifyRisk_WantToDie_ReturnsCrisis()
    {
        var result = await _classifier.ClassifyRiskAsync("I want to die");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Crisis));
    }

    [Test]
    public async Task ClassifyRisk_HurtMyself_ReturnsCrisis()
    {
        var result = await _classifier.ClassifyRiskAsync("I feel like I want to hurt myself");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Crisis));
    }

    [Test]
    public async Task ClassifyRisk_CrisisKeyword_HighConfidence()
    {
        var result = await _classifier.ClassifyRiskAsync("I want to kill myself");
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.9));
    }

    [Test]
    public async Task ClassifyRisk_CrisisKeyword_CaseInsensitive()
    {
        var result = await _classifier.ClassifyRiskAsync("SUICIDE is what I think about");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Crisis));
    }

    [Test]
    public async Task ClassifyRisk_SelfHarm_ReturnsCrisis()
    {
        var result = await _classifier.ClassifyRiskAsync("I've been doing self harm lately");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Crisis));
    }

    [Test]
    public async Task ClassifyRisk_Overdose_ReturnsCrisis()
    {
        var result = await _classifier.ClassifyRiskAsync("I'm thinking of an overdose");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Crisis));
    }

    // ─── Elevated ───────────────────────────────────────────────────────────

    [Test]
    public async Task ClassifyRisk_Hopeless_ReturnsElevated()
    {
        var result = await _classifier.ClassifyRiskAsync("I feel completely hopeless");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Elevated));
    }

    [Test]
    public async Task ClassifyRisk_Isolated_ReturnsElevated()
    {
        var result = await _classifier.ClassifyRiskAsync("I feel totally isolated from everyone");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Elevated));
    }

    [Test]
    public async Task ClassifyRisk_CantDoThis_ReturnsElevated()
    {
        var result = await _classifier.ClassifyRiskAsync("I can't do this anymore");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Elevated));
    }

    [Test]
    public async Task ClassifyRisk_ElevatedKeyword_CorrectConfidence()
    {
        var result = await _classifier.ClassifyRiskAsync("I feel so overwhelmed");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Elevated));
        Assert.That(result.Confidence, Is.EqualTo(0.80));
    }

    // ─── Distress ───────────────────────────────────────────────────────────

    [Test]
    public async Task ClassifyRisk_AnxietyKeyword_ReturnsDistress()
    {
        var result = await _classifier.ClassifyRiskAsync("I've been dealing with anxiety");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Distress));
    }

    [Test]
    public async Task ClassifyRisk_Depressed_ReturnsDistress()
    {
        var result = await _classifier.ClassifyRiskAsync("I have been feeling depressed");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Distress));
    }

    [Test]
    public async Task ClassifyRisk_Panic_ReturnsDistress()
    {
        var result = await _classifier.ClassifyRiskAsync("I had a panic attack today");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Distress));
    }

    [Test]
    public async Task ClassifyRisk_Stressed_ReturnsDistress()
    {
        var result = await _classifier.ClassifyRiskAsync("Work has me under a lot of stress today");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Distress));
    }

    [Test]
    public async Task ClassifyRisk_DistressKeyword_CorrectConfidence()
    {
        var result = await _classifier.ClassifyRiskAsync("Feeling sad today");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Distress));
        Assert.That(result.Confidence, Is.EqualTo(0.70));
    }

    // ─── Normal ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ClassifyRisk_NeutralMessage_ReturnsNormal()
    {
        var result = await _classifier.ClassifyRiskAsync("I had a good day today");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Normal));
    }

    [Test]
    public async Task ClassifyRisk_Normal_ConfidenceIsZero()
    {
        var result = await _classifier.ClassifyRiskAsync("The weather was nice today");
        Assert.That(result.Confidence, Is.EqualTo(0.0));
    }

    [Test]
    public async Task ClassifyRisk_Normal_ReasonIndicatesNoRisk()
    {
        var result = await _classifier.ClassifyRiskAsync("I went for a walk");
        Assert.That(result.Reason, Does.Contain("No risk").IgnoreCase.Or.Contains("no").IgnoreCase);
    }

    // ─── Self-harm context ───────────────────────────────────────────────────

    [Test]
    public async Task ClassifyRisk_SelfHarmContext_DangerousPhraseAndDistress_ReturnsCrisis()
    {
        // Uses a dangerous phrase + distress context keyword without matching any elevated/crisis arrays first
        var result = await _classifier.ClassifyRiskAsync(
            "Final message, I give up, nothing can fix any of this for me");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Crisis));
    }

    [Test]
    public async Task ClassifyRisk_SelfHarmContext_TooShortMessage_DoesNotTrigger()
    {
        var result = await _classifier.ClassifyRiskAsync("Goodbye hopeless");
        Assert.That(result.RiskLevel, Is.Not.EqualTo(RiskLevel.Crisis));
    }

    [Test]
    public async Task ClassifyRisk_DangerousPhrase_NoDistress_NotCrisis()
    {
        var result = await _classifier.ClassifyRiskAsync(
            "Goodbye everyone, I am going on vacation and everything is great");
        Assert.That(result.RiskLevel, Is.EqualTo(RiskLevel.Normal));
    }

    // ─── Reason string ──────────────────────────────────────────────────────

    [Test]
    public async Task ClassifyRisk_Crisis_ReasonIsPopulated()
    {
        var result = await _classifier.ClassifyRiskAsync("I want to harm myself");
        Assert.That(result.Reason, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ClassifyRisk_Elevated_ReasonIsPopulated()
    {
        var result = await _classifier.ClassifyRiskAsync("I feel hopeless");
        Assert.That(result.Reason, Is.Not.Null.And.Not.Empty);
    }
}
