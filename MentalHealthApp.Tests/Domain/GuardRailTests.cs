using MentalHealthApp.Domain.Entities;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Domain;

[TestFixture]
public class GuardRailTests
{
    [Test]
    public void GuardRail_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = "guard123";
        var patientId = "patient123";
        var keyword = "badword";
        var action = GuardRailAction.Remove;
        var replacement = "";
        var isActive = true;

        // Act
        var guardRail = new GuardRail
        {
            Id = id,
            PatientUserId = patientId,
            Keyword = keyword,
            Action = action,
            Replacement = replacement,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.That(guardRail.Id, Is.EqualTo(id));
        Assert.That(guardRail.PatientUserId, Is.EqualTo(patientId));
        Assert.That(guardRail.Keyword, Is.EqualTo(keyword));
        Assert.That(guardRail.Action, Is.EqualTo(action));
        Assert.That(guardRail.Replacement, Is.EqualTo(replacement));
        Assert.That(guardRail.IsActive, Is.EqualTo(isActive));
    }

    [Test]
    public void GuardRail_Action_ShouldAcceptValidValues()
    {
        // Arrange
        var guardRail = new GuardRail();

        // Act & Assert
        Assert.DoesNotThrow(() => guardRail.Action = GuardRailAction.Remove);
        Assert.DoesNotThrow(() => guardRail.Action = GuardRailAction.Replace);
    }

    [Test]
    public void GuardRail_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var guardRail = new GuardRail();

        // Assert
        Assert.That(guardRail.IsActive, Is.True);
    }
}