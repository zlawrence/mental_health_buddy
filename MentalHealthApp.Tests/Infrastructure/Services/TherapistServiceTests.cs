using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class TherapistServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IPatientProfileRepository> _patientProfileRepositoryMock;
    private Mock<ITherapistAccessRepository> _therapistAccessRepositoryMock;
    private Mock<IGuardRailRepository> _guardRailRepositoryMock;
    private Mock<IConversationRepository> _conversationRepositoryMock;
    private TherapistService _service;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _patientProfileRepositoryMock = new Mock<IPatientProfileRepository>();
        _therapistAccessRepositoryMock = new Mock<ITherapistAccessRepository>();
        _guardRailRepositoryMock = new Mock<IGuardRailRepository>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();

        _service = new TherapistService(
            _userRepositoryMock.Object,
            _patientProfileRepositoryMock.Object,
            _therapistAccessRepositoryMock.Object,
            _guardRailRepositoryMock.Object,
            _conversationRepositoryMock.Object);
    }

    private void SetupActiveAccess(string therapistId, string patientId)
    {
        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistAndPatientAsync(therapistId, patientId, default))
            .ReturnsAsync(new TherapistAccess
            {
                TherapistUserId = therapistId,
                PatientUserId = patientId,
                IsActive = true
            });
    }

    private void SetupNoAccess(string therapistId, string patientId)
    {
        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistAndPatientAsync(therapistId, patientId, default))
            .ReturnsAsync((TherapistAccess?)null);
    }

    private void SetupInactiveAccess(string therapistId, string patientId)
    {
        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistAndPatientAsync(therapistId, patientId, default))
            .ReturnsAsync(new TherapistAccess
            {
                TherapistUserId = therapistId,
                PatientUserId = patientId,
                IsActive = false
            });
    }

    // ─── GetAssignedPatientsAsync ────────────────────────────────────────────

    [Test]
    public async Task GetAssignedPatients_ReturnsAllAccessiblePatients()
    {
        var accessList = new List<TherapistAccess>
        {
            new TherapistAccess { PatientUserId = "patient1" },
            new TherapistAccess { PatientUserId = "patient2" }
        };

        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistIdAsync("therapist1", It.IsAny<bool>(), default))
            .ReturnsAsync(accessList);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("patient1", default))
            .ReturnsAsync(new User { Id = "patient1", Email = "p1@test.com", Username = "p1" });

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("patient2", default))
            .ReturnsAsync(new User { Id = "patient2", Email = "p2@test.com", Username = "p2" });

        _patientProfileRepositoryMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), default))
            .ReturnsAsync((PatientProfile?)null);

        var result = await _service.GetAssignedPatientsAsync("therapist1");

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAssignedPatients_NullUserForAccess_IsSkipped()
    {
        var accessList = new List<TherapistAccess>
        {
            new TherapistAccess { PatientUserId = "deleted-patient" },
            new TherapistAccess { PatientUserId = "patient1" }
        };

        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistIdAsync("therapist1", It.IsAny<bool>(), default))
            .ReturnsAsync(accessList);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("deleted-patient", default))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("patient1", default))
            .ReturnsAsync(new User { Id = "patient1", Email = "p1@test.com", Username = "p1" });

        _patientProfileRepositoryMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<string>(), default))
            .ReturnsAsync((PatientProfile?)null);

        var result = await _service.GetAssignedPatientsAsync("therapist1");

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].UserId, Is.EqualTo("patient1"));
    }

    // ─── GetPatientProfileAsync ──────────────────────────────────────────────

    [Test]
    public async Task GetPatientProfile_WithAccess_ReturnsProfile()
    {
        SetupActiveAccess("therapist1", "patient1");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("patient1", default))
            .ReturnsAsync(new User { Id = "patient1", Email = "p@test.com", Username = "patient1" });

        _patientProfileRepositoryMock
            .Setup(x => x.GetByUserIdAsync("patient1", default))
            .ReturnsAsync(new PatientProfile { PatientUserId = "patient1", ConversationRetentionDays = 30 });

        var result = await _service.GetPatientProfileAsync("therapist1", "patient1");

        Assert.That(result.UserId, Is.EqualTo("patient1"));
        Assert.That(result.ConversationRetentionDays, Is.EqualTo(30));
    }

    [Test]
    public void GetPatientProfile_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientProfileAsync("therapist1", "patient1"));
    }

    [Test]
    public void GetPatientProfile_InactiveAccess_ThrowsUnauthorized()
    {
        SetupInactiveAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientProfileAsync("therapist1", "patient1"));
    }

    [Test]
    public void GetPatientProfile_UserNotFound_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("patient1", default))
            .ReturnsAsync((User?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetPatientProfileAsync("therapist1", "patient1"));
    }

    // ─── GetPatientGuardRailsAsync ───────────────────────────────────────────

    [Test]
    public async Task GetPatientGuardRails_WithAccess_ReturnsGuardRails()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.GetByPatientAndTherapistAsync("patient1", "therapist1", default))
            .ReturnsAsync(new List<GuardRail>
            {
                new GuardRail { Id = "gr1", Keyword = "medication", IsActive = true }
            });

        var result = await _service.GetPatientGuardRailsAsync("therapist1", "patient1");

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Keyword, Is.EqualTo("medication"));
    }

    [Test]
    public void GetPatientGuardRails_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientGuardRailsAsync("therapist1", "patient1"));
    }

    // ─── GetPatientConversationsAsync ────────────────────────────────────────

    [Test]
    public async Task GetPatientConversations_WithAccess_ReturnsConversations()
    {
        SetupActiveAccess("therapist1", "patient1");

        _conversationRepositoryMock
            .Setup(x => x.GetByPatientIdAsync("patient1", true, default))
            .ReturnsAsync(new List<Conversation>
            {
                new Conversation { Id = "c1", PatientUserId = "patient1", Title = "Session 1" }
            });

        var result = await _service.GetPatientConversationsAsync("therapist1", "patient1");

        Assert.That(result.Count, Is.EqualTo(1));
    }

    // ─── UpdateGuardRailAsync ────────────────────────────────────────────────

    [Test]
    public async Task UpdateGuardRail_ValidRequest_UpdatesSuccessfully()
    {
        SetupActiveAccess("therapist1", "patient1");

        var guardRail = new GuardRail
        {
            Id = "gr1",
            PatientUserId = "patient1",
            TherapistUserId = "therapist1",
            Keyword = "old"
        };

        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync(guardRail);

        _guardRailRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<GuardRail>(), default))
            .Returns(Task.CompletedTask);

        await _service.UpdateGuardRailAsync("therapist1", "patient1", "gr1",
            new UpdateGuardRailRequest { Keyword = "new", Action = "Remove", IsActive = true });

        _guardRailRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<GuardRail>(g => g.Keyword == "new"), default),
            Times.Once);
    }

    [Test]
    public void UpdateGuardRail_GuardRailBelongsToDifferentPatient_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync(new GuardRail { Id = "gr1", PatientUserId = "other-patient" });

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateGuardRailAsync("therapist1", "patient1", "gr1",
                new UpdateGuardRailRequest { Keyword = "kw", Action = "Remove" }));
    }

    // ─── DeleteGuardRailAsync ────────────────────────────────────────────────

    [Test]
    public async Task DeleteGuardRail_ValidRequest_DeletesSuccessfully()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync(new GuardRail { Id = "gr1", PatientUserId = "patient1" });

        _guardRailRepositoryMock
            .Setup(x => x.DeleteAsync("gr1", default))
            .Returns(Task.CompletedTask);

        await _service.DeleteGuardRailAsync("therapist1", "patient1", "gr1");

        _guardRailRepositoryMock.Verify(x => x.DeleteAsync("gr1", default), Times.Once);
    }

    [Test]
    public void DeleteGuardRail_NotFound_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync((GuardRail?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteGuardRailAsync("therapist1", "patient1", "gr1"));
    }
}
