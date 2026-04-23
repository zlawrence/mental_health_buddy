using MentalHealthApp.Infrastructure.Services;
using NUnit.Framework;
using System.Security.Claims;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class JwtTokenServiceTests
{
    private const string SecretKey = "super-secret-key-for-unit-tests-at-least-32-chars!";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    private JwtTokenService _service;

    [SetUp]
    public void Setup()
    {
        _service = new JwtTokenService(SecretKey, expirationMinutes: 60, issuer: Issuer, audience: Audience);
    }

    // ─── GenerateToken ───────────────────────────────────────────────────────

    [Test]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = _service.GenerateToken("user1", "Patient", "patient@example.com");
        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateToken_TokenHasThreeParts()
    {
        var token = _service.GenerateToken("user1", "Patient", "patient@example.com");
        Assert.That(token.Split('.').Length, Is.EqualTo(3));
    }

    [Test]
    public void GenerateToken_DifferentUsersProduceDifferentTokens()
    {
        var t1 = _service.GenerateToken("user1", "Patient", "a@example.com");
        var t2 = _service.GenerateToken("user2", "Patient", "b@example.com");
        Assert.That(t1, Is.Not.EqualTo(t2));
    }

    // ─── ValidateToken ───────────────────────────────────────────────────────

    [Test]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        var token = _service.GenerateToken("user1", "Patient", "patient@example.com");
        var principal = _service.ValidateToken(token);
        Assert.That(principal, Is.Not.Null);
    }

    [Test]
    public void ValidateToken_ValidToken_ContainsNameIdentifierClaim()
    {
        var token = _service.GenerateToken("user42", "Patient", "patient@example.com");
        var principal = _service.ValidateToken(token);

        var userId = principal!.FindFirstValue(ClaimTypes.NameIdentifier);
        Assert.That(userId, Is.EqualTo("user42"));
    }

    [Test]
    public void ValidateToken_ValidToken_ContainsEmailClaim()
    {
        var token = _service.GenerateToken("user1", "Patient", "test@example.com");
        var principal = _service.ValidateToken(token);

        var email = principal!.FindFirstValue(ClaimTypes.Email);
        Assert.That(email, Is.EqualTo("test@example.com"));
    }

    [Test]
    public void ValidateToken_ValidToken_ContainsRoleClaim()
    {
        var token = _service.GenerateToken("user1", "Therapist", "therapist@example.com");
        var principal = _service.ValidateToken(token);

        var role = principal!.FindFirstValue(ClaimTypes.Role);
        Assert.That(role, Is.EqualTo("Therapist"));
    }

    [Test]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        var token = _service.GenerateToken("user1", "Patient", "patient@example.com");
        var tampered = token[..^5] + "XXXXX";
        var principal = _service.ValidateToken(tampered);
        Assert.That(principal, Is.Null);
    }

    [Test]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var principal = _service.ValidateToken("not.a.token");
        Assert.That(principal, Is.Null);
    }

    [Test]
    public void ValidateToken_WrongSecret_ReturnsNull()
    {
        var otherService = new JwtTokenService("different-secret-key-for-unit-tests-at-least-32!!", 60, Issuer, Audience);
        var token = otherService.GenerateToken("user1", "Patient", "patient@example.com");

        var principal = _service.ValidateToken(token);
        Assert.That(principal, Is.Null);
    }

    [Test]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        var expiredService = new JwtTokenService(SecretKey, expirationMinutes: -1, issuer: Issuer, audience: Audience);
        var token = expiredService.GenerateToken("user1", "Patient", "patient@example.com");

        var principal = _service.ValidateToken(token);
        Assert.That(principal, Is.Null);
    }

    [Test]
    public void ValidateToken_EmptyString_ReturnsNull()
    {
        var principal = _service.ValidateToken(string.Empty);
        Assert.That(principal, Is.Null);
    }

    [Test]
    public void GenerateToken_AdminRole_ValidatesCorrectly()
    {
        var token = _service.GenerateToken("admin1", "Admin", "admin@example.com");
        var principal = _service.ValidateToken(token);

        Assert.That(principal!.FindFirstValue(ClaimTypes.Role), Is.EqualTo("Admin"));
        Assert.That(principal!.FindFirstValue(ClaimTypes.NameIdentifier), Is.EqualTo("admin1"));
    }
}
