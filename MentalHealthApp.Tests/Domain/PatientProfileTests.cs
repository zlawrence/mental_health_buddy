using MentalHealthApp.Domain.Entities;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Domain;

[TestFixture]
public class PatientProfileTests
{
    [Test]
    public void PatientProfile_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = "profile123";
        var patientId = "patient123";
        var conversationRetentionDays = 30;

        // Act
        var profile = new PatientProfile
        {
            Id = id,
            PatientUserId = patientId,
            ConversationRetentionDays = conversationRetentionDays,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.That(profile.Id, Is.EqualTo(id));
        Assert.That(profile.PatientUserId, Is.EqualTo(patientId));
        Assert.That(profile.ConversationRetentionDays, Is.EqualTo(conversationRetentionDays));
    }

    [Test]
    public void PatientProfile_ConversationRetentionDays_ShouldHaveValidRange()
    {
        // Arrange
        var profile = new PatientProfile();

        // Act & Assert - Valid range
        Assert.DoesNotThrow(() => profile.ConversationRetentionDays = 1);
        Assert.DoesNotThrow(() => profile.ConversationRetentionDays = 365);
        Assert.DoesNotThrow(() => profile.ConversationRetentionDays = 30); // Default
    }
}
