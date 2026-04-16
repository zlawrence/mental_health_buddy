using MentalHealthApp.API.Controllers;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace MentalHealthApp.Tests.API.Controllers;

[TestFixture]
public class PatientsControllerTests
{
    private Mock<IPatientService> _patientServiceMock;
    private PatientsController _controller;

    [SetUp]
    public void Setup()
    {
        _patientServiceMock = new Mock<IPatientService>();
        _controller = new PatientsController(_patientServiceMock.Object);

        // Setup controller context with user claims
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "patient123"),
            new Claim(ClaimTypes.Role, "Patient")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public async Task GetMyProfile_ValidRequest_ShouldReturnProfile()
    {
        // Arrange
        var expectedProfile = new PatientProfileResponse
        {
            UserId = "patient123",
            Email = "patient@example.com",
            Username = "patientuser"
        };

        _patientServiceMock.Setup(x => x.GetPatientProfileAsync("patient123"))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(expectedProfile));
    }

    [Test]
    public async Task GetMyProfile_UnauthorizedAccessException_ShouldReturnUnauthorized()
    {
        // Arrange
        _patientServiceMock.Setup(x => x.GetPatientProfileAsync("patient123"))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task UpdateMyProfile_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new UpdatePatientProfileRequest
        {
            PhoneNumber = "+1234567890",
            ConversationRetentionDays = 30
        };

        _patientServiceMock.Setup(x => x.UpdatePatientProfileAsync("patient123", request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateMyProfile(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task GetMyGuardRails_ValidRequest_ShouldReturnGuardRails()
    {
        // Arrange
        List<GuardRailResponse> guardRails =
        [
            new GuardRailResponse { Id = "gr1", Keyword = "word1", Action = "remove" },
            new GuardRailResponse { Id = "gr2", Keyword = "word2", Action = "replace" }
        ];

        _patientServiceMock.Setup(x => x.GetPatientGuardRailsAsync("patient123"))
            .ReturnsAsync(guardRails);

        // Act
        var result = await _controller.GetMyGuardRails();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(guardRails));
    }

    [Test]
    public async Task CreateGuardRail_ValidRequest_ShouldReturnCreatedGuardRail()
    {
        // Arrange
        var request = new CreateGuardRailRequest
        {
            Keyword = "badword",
            Action = "remove",
            Replacement = ""
        };

        var createdGuardRail = new GuardRailResponse
        {
            Id = "gr123",
            Keyword = "badword",
            Action = "remove",
            IsActive = true
        };

        _patientServiceMock.Setup(x => x.CreateGuardRailAsync("patient123", request))
            .ReturnsAsync(createdGuardRail);

        // Act
        var result = await _controller.CreateGuardRail(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(createdGuardRail));
    }

    [Test]
    public async Task UpdateGuardRail_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var guardRailId = "gr123";
        var request = new UpdateGuardRailRequest
        {
            Keyword = "updatedword",
            Action = "replace",
            Replacement = "goodword",
            IsActive = true
        };

        _patientServiceMock.Setup(x => x.UpdateGuardRailAsync(guardRailId, request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateGuardRail(guardRailId, request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task DeleteGuardRail_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var guardRailId = "gr123";

        _patientServiceMock.Setup(x => x.DeleteGuardRailAsync(guardRailId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteGuardRail(guardRailId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
    }

    [Test]
    public async Task InviteTherapist_ValidRequest_ShouldReturnInvitation()
    {
        // Arrange
        var request = new InviteTherapistRequest
        {
            TherapistEmail = "therapist@example.com"
        };

        var invitationResponse = new TherapistInvitationResponse
        {
            Id = "inv123",
            TherapistEmail = "therapist@example.com",
            IsUsed = false
        };

        _patientServiceMock.Setup(x => x.InviteTherapistAsync("patient123", request))
            .ReturnsAsync(invitationResponse);

        // Act
        var result = await _controller.InviteTherapist(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(invitationResponse));
    }

    [Test]
    public void GetCurrentUserId_MissingClaim_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var controller = new PatientsController(_patientServiceMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act & Assert
        var wrapper = Assert.Throws<System.Reflection.TargetInvocationException>(() => controller.GetType().GetMethod("GetCurrentUserId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null));
        Assert.That(wrapper.InnerException, Is.InstanceOf<UnauthorizedAccessException>());
        Assert.That(wrapper.InnerException!.Message, Is.EqualTo("User identifier claim is missing."));
    }
}