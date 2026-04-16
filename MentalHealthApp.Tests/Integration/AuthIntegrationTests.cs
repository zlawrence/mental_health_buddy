using System.Net;
using System.Net.Http.Json;
using MentalHealthApp.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MentalHealthApp.Tests.Integration;

[TestFixture]
[NonParallelizable]
public class AuthIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task RegisterPatient_ValidRequest_ShouldRegisterSuccessfully()
    {
        // Arrange
        var registerRequest = new RegisterPatientRequest
        {
            Email = "newpatient@example.com",
            Username = "newpatient",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            PhoneNumber = "+1234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register-patient",
            registerRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<RegisterPatientResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Email, Is.EqualTo(registerRequest.Email));
        Assert.That(result.Username, Is.EqualTo(registerRequest.Username));
        Assert.That(result.UserId, Is.Not.Empty);
    }

    [Test]
    public async Task PatientLoginAndGetProfile_ValidFlow_ShouldReturnProfileData()
    {
        // Arrange - Register a new patient
        var email = $"patient_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid()}";
        var password = "TestPassword123!";

        var registerRequest = new RegisterPatientRequest
        {
            Email = email,
            Username = username,
            Password = password,
            ConfirmPassword = password
        };

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register-patient",
            registerRequest);
        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterPatientResponse>();
        var userId = registerResult!.UserId;

        // Act 1 - Login
        var loginRequest = new PatientLoginRequest
        {
            EmailOrUsername = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login-patient",
            loginRequest);

        // Assert 1 - Login should succeed
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.That(loginResult, Is.Not.Null);
        Assert.That(loginResult!.Token, Is.Not.Empty);
        Assert.That(loginResult.UserId, Is.EqualTo(userId));

        // Act 2 - Get profile with authenticated request
        var getProfileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/patients/me/profile");
        getProfileRequest.Headers.Add("Authorization", $"Bearer {loginResult.Token}");

        var profileResponse = await _client.SendAsync(getProfileRequest);

        // Assert 2 - Profile should be retrieved from database
        Assert.That(profileResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<PatientProfileResponse>();
        Assert.That(profileResult, Is.Not.Null);
        Assert.That(profileResult!.UserId, Is.EqualTo(userId));
        Assert.That(profileResult.Email, Is.EqualTo(email));
        Assert.That(profileResult.Username, Is.EqualTo(username));
    }

    [Test]
    public async Task PatientLogin_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange - Register a patient first
        var registerRequest = new RegisterPatientRequest
        {
            Email = "testuser@example.com",
            Username = "testuser",
            Password = "CorrectPassword123!",
            ConfirmPassword = "CorrectPassword123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register-patient", registerRequest);

        // Act - Try login with wrong password
        var loginRequest = new PatientLoginRequest
        {
            EmailOrUsername = "testuser@example.com",
            Password = "WrongPassword123!"
        };

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login-patient",
            loginRequest);

        // Assert
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/patients/me/profile");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetProfile_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Act
        var getProfileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/patients/me/profile");
        getProfileRequest.Headers.Add("Authorization", "Bearer invalid_token_here");

        var response = await _client.SendAsync(getProfileRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
