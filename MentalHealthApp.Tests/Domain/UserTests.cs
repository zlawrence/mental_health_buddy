using MentalHealthApp.Domain.Entities;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Domain;

[TestFixture]
public class UserTests
{
    [Test]
    public void User_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = "user123";
        var username = "testuser";
        var email = "test@example.com";
        var passwordHash = "hashedpassword";
        var role = UserRole.Patient;

        // Act
        var user = new User
        {
            Id = id,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.That(user.Id, Is.EqualTo(id));
        Assert.That(user.Username, Is.EqualTo(username));
        Assert.That(user.Email, Is.EqualTo(email));
        Assert.That(user.PasswordHash, Is.EqualTo(passwordHash));
        Assert.That(user.Role, Is.EqualTo(role));
        Assert.That(user.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(user.UpdatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void User_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.That(user.IsActive, Is.True);
    }
}