using AutoMapper;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Mappings;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class GuardRailServiceTests
{
    private Mock<IGuardRailRepository> _guardRailRepositoryMock;
    private Mock<ITherapistAccessRepository> _therapistAccessRepositoryMock;
    private IMapper _mapper;
    private GuardRailService _service;

    [SetUp]
    public void Setup()
    {
        _guardRailRepositoryMock = new Mock<IGuardRailRepository>();
        _therapistAccessRepositoryMock = new Mock<ITherapistAccessRepository>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _service = new GuardRailService(
            _guardRailRepositoryMock.Object,
            _therapistAccessRepositoryMock.Object,
            _mapper);
    }

    private void SetupAccess(string therapistId, string patientId, bool canManage = true, bool isActive = true)
    {
        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistAndPatientAsync(therapistId, patientId, default))
            .ReturnsAsync(new TherapistAccess
            {
                TherapistUserId = therapistId,
                PatientUserId = patientId,
                CanManageGuardRails = canManage,
                IsActive = isActive
            });
    }

    private void SetupNoAccess(string therapistId, string patientId)
    {
        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistAndPatientAsync(therapistId, patientId, default))
            .ReturnsAsync((TherapistAccess?)null);
    }

    // ─── CreateGuardRailAsync ────────────────────────────────────────────────

    [Test]
    public async Task CreateGuardRail_WithManagePermission_CreatesAndReturnsResponse()
    {
        SetupAccess("therapist1", "patient1");

        var createdGuardRail = new GuardRail
        {
            Id = "gr1",
            PatientUserId = "patient1",
            TherapistUserId = "therapist1",
            Keyword = "medication",
            Action = GuardRailAction.Remove,
            IsActive = true
        };

        _guardRailRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GuardRail>(), default))
            .ReturnsAsync(createdGuardRail);

        var result = await _service.CreateGuardRailAsync("therapist1", new CreateGuardRailRequest
        {
            PatientId = "patient1",
            Keyword = "medication",
            Action = "remove"
        });

        Assert.That(result.Id, Is.EqualTo("gr1"));
        Assert.That(result.Keyword, Is.EqualTo("medication"));
        Assert.That(result.Action, Is.EqualTo("Remove"));
    }

    [Test]
    public async Task CreateGuardRail_SetsTherapistIdAndIsActive()
    {
        SetupAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GuardRail>(), default))
            .ReturnsAsync(new GuardRail { Id = "gr1" });

        await _service.CreateGuardRailAsync("therapist1", new CreateGuardRailRequest
        {
            PatientId = "patient1",
            Keyword = "alcohol",
            Action = "remove"
        });

        _guardRailRepositoryMock.Verify(x => x.AddAsync(It.Is<GuardRail>(g =>
            g.TherapistUserId == "therapist1" &&
            g.IsActive == true), default), Times.Once);
    }

    [Test]
    public void CreateGuardRail_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateGuardRailAsync("therapist1", new CreateGuardRailRequest
            {
                PatientId = "patient1",
                Keyword = "kw",
                Action = "remove"
            }));
    }

    [Test]
    public void CreateGuardRail_NoManagePermission_ThrowsUnauthorized()
    {
        SetupAccess("therapist1", "patient1", canManage: false);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateGuardRailAsync("therapist1", new CreateGuardRailRequest
            {
                PatientId = "patient1",
                Keyword = "kw",
                Action = "remove"
            }));
    }

    [Test]
    public void CreateGuardRail_InactiveAccess_ThrowsUnauthorized()
    {
        SetupAccess("therapist1", "patient1", canManage: true, isActive: false);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateGuardRailAsync("therapist1", new CreateGuardRailRequest
            {
                PatientId = "patient1",
                Keyword = "kw",
                Action = "remove"
            }));
    }

    // ─── UpdateGuardRailAsync ────────────────────────────────────────────────

    [Test]
    public async Task UpdateGuardRail_OwnedByTherapist_UpdatesAndReturnsResponse()
    {
        var guardRail = new GuardRail
        {
            Id = "gr1",
            TherapistUserId = "therapist1",
            PatientUserId = "patient1",
            Keyword = "old-keyword",
            Action = GuardRailAction.Remove,
            IsActive = true
        };

        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync(guardRail);

        _guardRailRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<GuardRail>(), default))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateGuardRailAsync("gr1", "therapist1", new UpdateGuardRailRequest
        {
            Keyword = "new-keyword",
            Action = "replace",
            Replacement = "safe-word",
            IsActive = true
        });

        Assert.That(result.Keyword, Is.EqualTo("new-keyword"));
        Assert.That(result.Action, Is.EqualTo("Replace"));
        Assert.That(result.Replacement, Is.EqualTo("safe-word"));
    }

    [Test]
    public void UpdateGuardRail_NotFound_ThrowsUnauthorized()
    {
        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("missing", default))
            .ReturnsAsync((GuardRail?)null);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateGuardRailAsync("missing", "therapist1", new UpdateGuardRailRequest
            {
                Keyword = "kw",
                Action = "remove"
            }));
    }

    [Test]
    public void UpdateGuardRail_BelongsToOtherTherapist_ThrowsUnauthorized()
    {
        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync(new GuardRail { Id = "gr1", TherapistUserId = "other-therapist" });

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateGuardRailAsync("gr1", "therapist1", new UpdateGuardRailRequest
            {
                Keyword = "kw",
                Action = "remove"
            }));
    }

    // ─── DeleteGuardRailAsync ────────────────────────────────────────────────

    [Test]
    public async Task DeleteGuardRail_OwnedByTherapist_DeletesSuccessfully()
    {
        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync(new GuardRail { Id = "gr1", TherapistUserId = "therapist1" });

        _guardRailRepositoryMock
            .Setup(x => x.DeleteAsync("gr1", default))
            .Returns(Task.CompletedTask);

        await _service.DeleteGuardRailAsync("gr1", "therapist1");

        _guardRailRepositoryMock.Verify(x => x.DeleteAsync("gr1", default), Times.Once);
    }

    [Test]
    public void DeleteGuardRail_NotFound_ThrowsUnauthorized()
    {
        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("missing", default))
            .ReturnsAsync((GuardRail?)null);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteGuardRailAsync("missing", "therapist1"));
    }

    [Test]
    public void DeleteGuardRail_BelongsToOtherTherapist_ThrowsUnauthorized()
    {
        _guardRailRepositoryMock
            .Setup(x => x.GetByIdAsync("gr1", default))
            .ReturnsAsync(new GuardRail { Id = "gr1", TherapistUserId = "different-therapist" });

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteGuardRailAsync("gr1", "therapist1"));
    }

    // ─── GetPatientGuardRailsAsync ───────────────────────────────────────────

    [Test]
    public async Task GetPatientGuardRails_WithManagePermission_ReturnsGuardRails()
    {
        SetupAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.GetByPatientAndTherapistAsync("patient1", "therapist1", default))
            .ReturnsAsync(new List<GuardRail>
            {
                new GuardRail { Id = "gr1", Keyword = "alcohol", Action = GuardRailAction.Remove, IsActive = true },
                new GuardRail { Id = "gr2", Keyword = "drug", Action = GuardRailAction.Replace, Replacement = "[substance]", IsActive = false }
            });

        var result = await _service.GetPatientGuardRailsAsync("patient1", "therapist1");

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Keyword, Is.EqualTo("alcohol"));
        Assert.That(result[1].Action, Is.EqualTo("Replace"));
    }

    [Test]
    public void GetPatientGuardRails_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientGuardRailsAsync("patient1", "therapist1"));
    }

    [Test]
    public async Task GetPatientGuardRails_EmptyList_ReturnsEmpty()
    {
        SetupAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.GetByPatientAndTherapistAsync("patient1", "therapist1", default))
            .ReturnsAsync(new List<GuardRail>());

        var result = await _service.GetPatientGuardRailsAsync("patient1", "therapist1");

        Assert.That(result, Is.Empty);
    }
}
