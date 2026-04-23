using MentalHealthApp.Infrastructure.Services;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class PasswordHashingServiceTests
{
    private PasswordHashingService _service;

    [SetUp]
    public void Setup()
    {
        _service = new PasswordHashingService();
    }

    [Test]
    public void HashPassword_ReturnsNonEmptyString()
    {
        var hash = _service.HashPassword("MyPassword123!");
        Assert.That(hash, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void HashPassword_SamePasswordProducesDifferentHashes()
    {
        var h1 = _service.HashPassword("SamePassword");
        var h2 = _service.HashPassword("SamePassword");
        Assert.That(h1, Is.Not.EqualTo(h2));
    }

    [Test]
    public void HashPassword_HashIsBcryptFormat()
    {
        var hash = _service.HashPassword("TestPassword");
        Assert.That(hash, Does.StartWith("$2"));
    }

    [Test]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        const string password = "Correct$Password99";
        var hash = _service.HashPassword(password);
        Assert.That(_service.VerifyPassword(password, hash), Is.True);
    }

    [Test]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _service.HashPassword("CorrectPassword");
        Assert.That(_service.VerifyPassword("WrongPassword", hash), Is.False);
    }

    [Test]
    public void VerifyPassword_EmptyPassword_WhenHashedAndVerified_ReturnsTrue()
    {
        var hash = _service.HashPassword(string.Empty);
        Assert.That(_service.VerifyPassword(string.Empty, hash), Is.True);
    }

    [Test]
    public void VerifyPassword_EmptyPassword_AgainstNonEmptyHash_ReturnsFalse()
    {
        var hash = _service.HashPassword("NonEmptyPassword");
        Assert.That(_service.VerifyPassword(string.Empty, hash), Is.False);
    }

    [Test]
    public void VerifyPassword_CaseSensitive()
    {
        var hash = _service.HashPassword("Password");
        Assert.That(_service.VerifyPassword("password", hash), Is.False);
        Assert.That(_service.VerifyPassword("PASSWORD", hash), Is.False);
    }

    [Test]
    public void HashPassword_SpecialCharacters_HandledCorrectly()
    {
        const string password = "P@$$w0rd!#%&*()";
        var hash = _service.HashPassword(password);
        Assert.That(_service.VerifyPassword(password, hash), Is.True);
    }
}
