using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Application.Services;

[TestFixture]
public class PatientServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IPatientProfileRepository> _patientProfileRepositoryMock;
    private Mock<IGuardRailRepository> _guardRailRepositoryMock;
    private Mock<ITherapistInvitationRepository> _invitationRepositoryMock;
    private Mock<IEmailQueue> _emailQueueMock;
    private PatientService _patientService;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _patientProfileRepositoryMock = new Mock<IPatientProfileRepository>();
        _guardRailRepositoryMock = new Mock<IGuardRailRepository>();
        _invitationRepositoryMock = new Mock<ITherapistInvitationRepository>();
        _emailQueueMock = new Mock<IEmailQueue>();

        _patientService = new PatientService(
            _userRepositoryMock.Object,
            _patientProfileRepositoryMock.Object,
            _guardRailRepositoryMock.Object,
            _invitationRepositoryMock.Object,
            _emailQueueMock.Object,
            "https://localhost");
    }

    [Test]
    public async Task GetPatientProfileAsync_ExistingProfile_ShouldReturnProfileResponse()
    {
        // Arrange
        var patientId = "patient123";
        var user = new User
        {
            Id = patientId,
            Email = "patient@example.com",
            Username = "patientuser",
            PhoneNumber = "+1234567890",
            Role = UserRole.Patient
        };

        var profile = new PatientProfile
        {
            PatientUserId = patientId,
            ConversationRetentionDays = 30,
            TherapistUserIds = ["therapist123"]
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _patientProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _patientService.GetPatientProfileAsync(patientId);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(patientId));
        Assert.That(result.Email, Is.EqualTo(user.Email));
        Assert.That(result.Username, Is.EqualTo(user.Username));
        Assert.That(result.PhoneNumber, Is.EqualTo(user.PhoneNumber));
        Assert.That(result.ConversationRetentionDays, Is.EqualTo(profile.ConversationRetentionDays));
        Assert.That(result.TherapistIds, Is.EqualTo(profile.TherapistUserIds));
    }

    [Test]
    public async Task GetPatientProfileAsync_NoExistingProfile_ShouldCreateDefaultProfile()
    {
        // Arrange
        var patientId = "patient123";
        var user = new User
        {
            Id = patientId,
            Email = "patient@example.com",
            Username = "patientuser",
            Role = UserRole.Patient
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _patientProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PatientProfile?)null);
        _patientProfileRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PatientProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientProfile());

        // Act
        var result = await _patientService.GetPatientProfileAsync(patientId);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(patientId));
        Assert.That(result.ConversationRetentionDays, Is.EqualTo(-1)); // Default value
        _patientProfileRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PatientProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void GetPatientProfileAsync_PatientNotFound_ShouldThrowException()
    {
        // Arrange
        var patientId = "nonexistent";
        _userRepositoryMock.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(
            () => _patientService.GetPatientProfileAsync(patientId));
        Assert.That(exception.Message, Is.EqualTo("Patient not found"));
    }

    [Test]
    public async Task UpdatePatientProfileAsync_ValidRequest_ShouldUpdateProfile()
    {
        // Arrange
        var patientId = "patient123";
        var request = new UpdatePatientProfileRequest
        {
            PhoneNumber = "+0987654321",
            ConversationRetentionDays = 60
        };

        var user = new User
        {
            Id = patientId,
            Email = "patient@example.com",
            Role = UserRole.Patient
        };

        var profile = new PatientProfile
        {
            PatientUserId = patientId,
            ConversationRetentionDays = 30
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _patientProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _patientProfileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PatientProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _patientService.UpdatePatientProfileAsync(patientId, request);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => u.PhoneNumber == request.PhoneNumber), It.IsAny<CancellationToken>()), Times.Once);
        _patientProfileRepositoryMock.Verify(x => x.UpdateAsync(It.Is<PatientProfile>(p => p.ConversationRetentionDays == request.ConversationRetentionDays), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateGuardRailAsync_ValidRequest_ShouldCreateGuardRail()
    {
        // Arrange
        var patientId = "patient123";
        var request = new CreateGuardRailRequest
        {
            Keyword = "badword",
            Action = "remove",
            Replacement = ""
        };

        var user = new User
        {
            Id = patientId,
            Role = UserRole.Patient
        };

        var profile = new PatientProfile
        {
            PatientUserId = patientId,
            TherapistUserIds = ["therapist123"]
        };

        var createdGuardRail = new GuardRail
        {
            Id = "guard123",
            PatientUserId = patientId,
            TherapistUserId = "therapist123",
            Keyword = request.Keyword,
            Action = GuardRailAction.Remove,
            Replacement = request.Replacement,
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _patientProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _guardRailRepositoryMock.Setup(x => x.AddAsync(It.IsAny<GuardRail>(), It.IsAny<CancellationToken>()))
            .Callback<GuardRail, CancellationToken>((gr, _) => gr.Id = "guard123")
            .ReturnsAsync(new GuardRail());

        // Act
        var result = await _patientService.CreateGuardRailAsync(patientId, request);

        // Assert
        Assert.That(result.Id, Is.EqualTo("guard123"));
        Assert.That(result.PatientId, Is.EqualTo(patientId));
        Assert.That(result.Keyword, Is.EqualTo(request.Keyword));
        Assert.That(result.Action, Is.EqualTo("remove"));
        Assert.That(result.IsActive, Is.True);
    }

    [Test]
    public void CreateGuardRailAsync_InvalidAction_ShouldThrowException()
    {
        // Arrange
        var patientId = "patient123";
        var request = new CreateGuardRailRequest
        {
            Keyword = "badword",
            Action = "invalid",
            Replacement = ""
        };

        var user = new User
        {
            Id = patientId,
            Role = UserRole.Patient
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(
            () => _patientService.CreateGuardRailAsync(patientId, request));
        Assert.That(exception.Message, Is.EqualTo("Invalid action. Must be 'remove' or 'replace'"));
    }

    [Test]
    public async Task UpdateGuardRailAsync_ValidRequest_ShouldUpdateGuardRail()
    {
        // Arrange
        var guardRailId = "guard123";
        var request = new UpdateGuardRailRequest
        {
            Keyword = "updatedword",
            Action = "replace",
            Replacement = "goodword",
            IsActive = false
        };

        var guardRail = new GuardRail
        {
            Id = guardRailId,
            Keyword = "badword",
            Action = GuardRailAction.Remove,
            IsActive = true
        };

        _guardRailRepositoryMock.Setup(x => x.GetByIdAsync(guardRailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardRail);
        _guardRailRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<GuardRail>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _patientService.UpdateGuardRailAsync(guardRailId, request);

        // Assert
        _guardRailRepositoryMock.Verify(x => x.UpdateAsync(It.Is<GuardRail>(gr =>
            gr.Keyword == request.Keyword &&
            gr.Action == GuardRailAction.Replace &&
            gr.Replacement == request.Replacement &&
            gr.IsActive == request.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateGuardRailAsync_GuardRailNotFound_ShouldThrowException()
    {
        // Arrange
        var guardRailId = "nonexistent";
        var request = new UpdateGuardRailRequest
        {
            Keyword = "test",
            Action = "remove",
            IsActive = true
        };

        _guardRailRepositoryMock.Setup(x => x.GetByIdAsync(guardRailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuardRail?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(
            () => _patientService.UpdateGuardRailAsync(guardRailId, request));
        Assert.That(exception.Message, Is.EqualTo("Guard rail not found"));
    }

    [Test]
    public async Task InviteTherapistAsync_ValidRequest_ShouldCreateInvitation()
    {
        // Arrange
        var patientId = "patient123";
        var request = new InviteTherapistRequest
        {
            TherapistEmail = "therapist@example.com"
        };

        var user = new User
        {
            Id = patientId,
            Role = UserRole.Patient
        };

        var createdInvitation = new TherapistInvitation
        {
            Id = "invitation123",
            PatientUserId = patientId,
            TherapistEmail = request.TherapistEmail,
            IsUsed = false
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _invitationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TherapistInvitation>(), It.IsAny<CancellationToken>()))
            .Callback<TherapistInvitation, CancellationToken>((inv, _) => inv.Id = "invitation123")
            .ReturnsAsync(new TherapistInvitation());

        // Act
        var result = await _patientService.InviteTherapistAsync(patientId, request);

        // Assert
        Assert.That(result.Id, Is.EqualTo("invitation123"));
        Assert.That(result.TherapistEmail, Is.EqualTo(request.TherapistEmail));
        Assert.That(result.IsUsed, Is.False);
        _invitationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TherapistInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
