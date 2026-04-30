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
    private Mock<IConversationMessageRepository> _conversationMessageRepositoryMock;
    private TherapistService _service;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _patientProfileRepositoryMock = new Mock<IPatientProfileRepository>();
        _therapistAccessRepositoryMock = new Mock<ITherapistAccessRepository>();
        _guardRailRepositoryMock = new Mock<IGuardRailRepository>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _conversationMessageRepositoryMock = new Mock<IConversationMessageRepository>();

        _service = new TherapistService(
            _userRepositoryMock.Object,
            _patientProfileRepositoryMock.Object,
            _therapistAccessRepositoryMock.Object,
            _guardRailRepositoryMock.Object,
            _conversationRepositoryMock.Object,
            _conversationMessageRepositoryMock.Object);
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

    private void SetupAccessWithPermissions(string therapistId, string patientId, bool canViewChats, bool canManageGuardRails)
    {
        _therapistAccessRepositoryMock
            .Setup(x => x.GetByTherapistAndPatientAsync(therapistId, patientId, default))
            .ReturnsAsync(new TherapistAccess
            {
                TherapistUserId = therapistId,
                PatientUserId = patientId,
                IsActive = true,
                CanViewChats = canViewChats,
                CanManageGuardRails = canManageGuardRails
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

    [Test]
    public void UpdateGuardRail_CanManageGuardRailsFalse_ThrowsUnauthorized()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: true, canManageGuardRails: false);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateGuardRailAsync("therapist1", "patient1", "gr1",
                new UpdateGuardRailRequest { Keyword = "kw", Action = "Remove" }));
    }

    [Test]
    public void DeleteGuardRail_CanManageGuardRailsFalse_ThrowsUnauthorized()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: true, canManageGuardRails: false);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteGuardRailAsync("therapist1", "patient1", "gr1"));
    }

    // ─── GetTherapistAccessAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetTherapistAccess_ActiveAccess_ReturnsBothPermissionsTrue()
    {
        SetupActiveAccess("therapist1", "patient1");

        var result = await _service.GetTherapistAccessAsync("therapist1", "patient1");

        Assert.That(result.CanViewChats, Is.True);
        Assert.That(result.CanManageGuardRails, Is.True);
    }

    [Test]
    public async Task GetTherapistAccess_CanViewChatsFalse_ReturnsCorrectFlag()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: false, canManageGuardRails: true);

        var result = await _service.GetTherapistAccessAsync("therapist1", "patient1");

        Assert.That(result.CanViewChats, Is.False);
        Assert.That(result.CanManageGuardRails, Is.True);
    }

    [Test]
    public async Task GetTherapistAccess_CanManageGuardRailsFalse_ReturnsCorrectFlag()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: true, canManageGuardRails: false);

        var result = await _service.GetTherapistAccessAsync("therapist1", "patient1");

        Assert.That(result.CanViewChats, Is.True);
        Assert.That(result.CanManageGuardRails, Is.False);
    }

    [Test]
    public async Task GetTherapistAccess_BothPermissionsFalse_ReturnsCorrectFlags()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: false, canManageGuardRails: false);

        var result = await _service.GetTherapistAccessAsync("therapist1", "patient1");

        Assert.That(result.CanViewChats, Is.False);
        Assert.That(result.CanManageGuardRails, Is.False);
    }

    [Test]
    public void GetTherapistAccess_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetTherapistAccessAsync("therapist1", "patient1"));
    }

    [Test]
    public void GetTherapistAccess_InactiveAccess_ThrowsUnauthorized()
    {
        SetupInactiveAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetTherapistAccessAsync("therapist1", "patient1"));
    }

    // ─── CreateGuardRailAsync ────────────────────────────────────────────────

    [Test]
    public async Task CreateGuardRail_RemoveAction_CreatesSuccessfully()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GuardRail>(), default))
            .ReturnsAsync((GuardRail gr, CancellationToken _) => gr);

        var request = new TherapistCreateGuardRailRequest { Keyword = "medication", Action = "remove" };

        var result = await _service.CreateGuardRailAsync("therapist1", "patient1", request);

        Assert.That(result.Keyword, Is.EqualTo("medication"));
        Assert.That(result.Action, Is.EqualTo("Remove"));
        Assert.That(result.PatientId, Is.EqualTo("patient1"));
        Assert.That(result.TherapistId, Is.EqualTo("therapist1"));
        Assert.That(result.IsActive, Is.True);
    }

    [Test]
    public async Task CreateGuardRail_ReplaceAction_CreatesWithReplacement()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GuardRail>(), default))
            .ReturnsAsync((GuardRail gr, CancellationToken _) => gr);

        var request = new TherapistCreateGuardRailRequest
        {
            Keyword = "suicide",
            Action = "replace",
            Replacement = "please reach out for support"
        };

        var result = await _service.CreateGuardRailAsync("therapist1", "patient1", request);

        Assert.That(result.Action, Is.EqualTo("Replace"));
        Assert.That(result.Replacement, Is.EqualTo("please reach out for support"));
    }

    [Test]
    public async Task CreateGuardRail_RemoveAction_SetsReplacementNull()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GuardRail>(), default))
            .ReturnsAsync((GuardRail gr, CancellationToken _) => gr);

        var request = new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "remove" };

        var result = await _service.CreateGuardRailAsync("therapist1", "patient1", request);

        Assert.That(result.Replacement, Is.Null);
    }

    [Test]
    public async Task CreateGuardRail_KeywordIsTrimed_StoresWithoutWhitespace()
    {
        SetupActiveAccess("therapist1", "patient1");

        GuardRail? capturedRail = null;
        _guardRailRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GuardRail>(), default))
            .Callback<GuardRail, CancellationToken>((gr, _) => capturedRail = gr)
            .ReturnsAsync((GuardRail gr, CancellationToken _) => gr);

        await _service.CreateGuardRailAsync("therapist1", "patient1",
            new TherapistCreateGuardRailRequest { Keyword = "  medication  ", Action = "remove" });

        Assert.That(capturedRail!.Keyword, Is.EqualTo("medication"));
    }

    [Test]
    public void CreateGuardRail_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateGuardRailAsync("therapist1", "patient1",
                new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "remove" }));
    }

    [Test]
    public void CreateGuardRail_InactiveAccess_ThrowsUnauthorized()
    {
        SetupInactiveAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateGuardRailAsync("therapist1", "patient1",
                new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "remove" }));
    }

    [Test]
    public void CreateGuardRail_CanManageGuardRailsFalse_ThrowsUnauthorized()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: true, canManageGuardRails: false);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateGuardRailAsync("therapist1", "patient1",
                new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "remove" }));
    }

    [Test]
    public void CreateGuardRail_InvalidAction_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateGuardRailAsync("therapist1", "patient1",
                new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "invalidAction" }));
    }

    [Test]
    public void CreateGuardRail_ReplaceActionWithoutReplacement_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateGuardRailAsync("therapist1", "patient1",
                new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "replace", Replacement = null }));
    }

    [Test]
    public void CreateGuardRail_ReplaceActionWithWhitespaceReplacement_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateGuardRailAsync("therapist1", "patient1",
                new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "replace", Replacement = "   " }));
    }

    [Test]
    public async Task CreateGuardRail_CallsAddAsync_Once()
    {
        SetupActiveAccess("therapist1", "patient1");

        _guardRailRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<GuardRail>(), default))
            .ReturnsAsync((GuardRail gr, CancellationToken _) => gr);

        await _service.CreateGuardRailAsync("therapist1", "patient1",
            new TherapistCreateGuardRailRequest { Keyword = "kw", Action = "remove" });

        _guardRailRepositoryMock.Verify(x => x.AddAsync(It.IsAny<GuardRail>(), default), Times.Once);
    }

    // ─── GetPatientConversationMessagesAsync ─────────────────────────────────

    [Test]
    public async Task GetPatientConversationMessages_ValidConversation_ReturnsMessages()
    {
        SetupActiveAccess("therapist1", "patient1");

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(new Conversation { Id = "conv1", PatientUserId = "patient1" });

        _conversationMessageRepositoryMock
            .Setup(x => x.GetByConversationIdAsync("conv1", default))
            .ReturnsAsync(new List<ConversationMessage>
            {
                new ConversationMessage { Id = "msg1", ConversationId = "conv1", Role = MessageRole.User, Content = "Hello" },
                new ConversationMessage { Id = "msg2", ConversationId = "conv1", Role = MessageRole.Assistant, Content = "Hi there" }
            });

        var result = await _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1");

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetPatientConversationMessages_MapsRoleAndContentCorrectly()
    {
        SetupActiveAccess("therapist1", "patient1");

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(new Conversation { Id = "conv1", PatientUserId = "patient1" });

        var timestamp = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        _conversationMessageRepositoryMock
            .Setup(x => x.GetByConversationIdAsync("conv1", default))
            .ReturnsAsync(new List<ConversationMessage>
            {
                new ConversationMessage
                {
                    Id = "msg1", ConversationId = "conv1",
                    Role = MessageRole.User, Content = "How are you?",
                    Timestamp = timestamp
                }
            });

        var result = await _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1");

        Assert.That(result[0].Role, Is.EqualTo("User"));
        Assert.That(result[0].Content, Is.EqualTo("How are you?"));
        Assert.That(result[0].Timestamp, Is.EqualTo(timestamp));
        Assert.That(result[0].ConversationId, Is.EqualTo("conv1"));
    }

    [Test]
    public async Task GetPatientConversationMessages_AssistantRole_MappedCorrectly()
    {
        SetupActiveAccess("therapist1", "patient1");

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(new Conversation { Id = "conv1", PatientUserId = "patient1" });

        _conversationMessageRepositoryMock
            .Setup(x => x.GetByConversationIdAsync("conv1", default))
            .ReturnsAsync(new List<ConversationMessage>
            {
                new ConversationMessage { Id = "msg1", Role = MessageRole.Assistant, Content = "I'm here to help." }
            });

        var result = await _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1");

        Assert.That(result[0].Role, Is.EqualTo("Assistant"));
    }

    [Test]
    public async Task GetPatientConversationMessages_EmptyConversation_ReturnsEmptyList()
    {
        SetupActiveAccess("therapist1", "patient1");

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(new Conversation { Id = "conv1", PatientUserId = "patient1" });

        _conversationMessageRepositoryMock
            .Setup(x => x.GetByConversationIdAsync("conv1", default))
            .ReturnsAsync(new List<ConversationMessage>());

        var result = await _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetPatientConversationMessages_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1"));
    }

    [Test]
    public void GetPatientConversationMessages_InactiveAccess_ThrowsUnauthorized()
    {
        SetupInactiveAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1"));
    }

    [Test]
    public void GetPatientConversationMessages_CanViewChatsFalse_ThrowsUnauthorized()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: false, canManageGuardRails: true);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1"));
    }

    [Test]
    public void GetPatientConversationMessages_ConversationNotFound_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync((Conversation?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1"));
    }

    [Test]
    public void GetPatientConversationMessages_ConversationBelongsToDifferentPatient_ThrowsInvalidOperation()
    {
        SetupActiveAccess("therapist1", "patient1");

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(new Conversation { Id = "conv1", PatientUserId = "other-patient" });

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetPatientConversationMessagesAsync("therapist1", "patient1", "conv1"));
    }

    // ─── GetPatientConversationsAsync — permission enforcement ───────────────

    [Test]
    public void GetPatientConversations_CanViewChatsFalse_ThrowsUnauthorized()
    {
        SetupAccessWithPermissions("therapist1", "patient1", canViewChats: false, canManageGuardRails: true);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientConversationsAsync("therapist1", "patient1"));
    }

    [Test]
    public void GetPatientConversations_NoAccess_ThrowsUnauthorized()
    {
        SetupNoAccess("therapist1", "patient1");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetPatientConversationsAsync("therapist1", "patient1"));
    }
}
