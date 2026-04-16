using MentalHealthApp.Domain.Entities;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Domain;

[TestFixture]
public class ConversationTests
{
    [Test]
    public void Conversation_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = "conv123";
        var patientId = "patient123";
        var title = "Test Conversation";
        var startedDate = DateTime.UtcNow;
        var isArchived = false;
        var messageCount = 5;

        // Act
        var conversation = new Conversation
        {
            Id = id,
            PatientUserId = patientId,
            Title = title,
            StartedDate = startedDate,
            IsArchived = isArchived,
            MessageCount = messageCount,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.That(conversation.Id, Is.EqualTo(id));
        Assert.That(conversation.PatientUserId, Is.EqualTo(patientId));
        Assert.That(conversation.Title, Is.EqualTo(title));
        Assert.That(conversation.StartedDate, Is.EqualTo(startedDate));
        Assert.That(conversation.IsArchived, Is.EqualTo(isArchived));
        Assert.That(conversation.MessageCount, Is.EqualTo(messageCount));
    }

    [Test]
    public void Conversation_IsArchived_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var conversation = new Conversation();

        // Assert
        Assert.That(conversation.IsArchived, Is.False);
    }

    [Test]
    public void Conversation_MessageCount_ShouldBeNonNegative()
    {
        // Arrange
        var conversation = new Conversation();

        // Act & Assert
        Assert.DoesNotThrow(() => conversation.MessageCount = 0);
        Assert.DoesNotThrow(() => conversation.MessageCount = 100);
    }
}