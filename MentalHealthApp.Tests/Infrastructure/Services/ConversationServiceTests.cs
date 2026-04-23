using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Fx;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class ConversationServiceTests
{
    private Mock<IConversationRepository> _conversationRepositoryMock;
    private Mock<IConversationMessageRepository> _messageRepositoryMock;
    private Mock<IGuardRailRepository> _guardRailRepositoryMock;
    private Mock<IRateLimitingService> _rateLimitingServiceMock;
    private Mock<IRiskClassifier> _riskClassifierMock;
    private Mock<ILLMTherapyService> _claudeServiceMock;
    private Mock<IGuardRailEnforcementService> _guardRailEnforcementServiceMock;
    private ConversationService _service;

    [SetUp]
    public void Setup()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _messageRepositoryMock = new Mock<IConversationMessageRepository>();
        _guardRailRepositoryMock = new Mock<IGuardRailRepository>();
        _rateLimitingServiceMock = new Mock<IRateLimitingService>();
        _riskClassifierMock = new Mock<IRiskClassifier>();
        _claudeServiceMock = new Mock<ILLMTherapyService>();
        _guardRailEnforcementServiceMock = new Mock<IGuardRailEnforcementService>();

        _service = new ConversationService(
            _conversationRepositoryMock.Object,
            _messageRepositoryMock.Object,
            _guardRailRepositoryMock.Object,
            _rateLimitingServiceMock.Object,
            _riskClassifierMock.Object,
            _claudeServiceMock.Object,
            _guardRailEnforcementServiceMock.Object);
    }

    // ─── CreateConversationAsync ─────────────────────────────────────────────

    [Test]
    public async Task CreateConversation_ValidRequest_ReturnsResponse()
    {
        var conversation = new Conversation
        {
            Id = "conv1",
            PatientUserId = "patient1",
            Title = "My Session",
            StartedDate = DateTime.UtcNow,
            IsArchived = false,
            MessageCount = 0
        };

        _conversationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Conversation>(), default))
            .ReturnsAsync(conversation);

        var result = await _service.CreateConversationAsync("patient1", new CreateConversationRequest { Title = "My Session" });

        Assert.That(result.Id, Is.EqualTo("conv1"));
        Assert.That(result.PatientId, Is.EqualTo("patient1"));
        Assert.That(result.Title, Is.EqualTo("My Session"));
        Assert.That(result.IsArchived, Is.False);
        Assert.That(result.MessageCount, Is.EqualTo(0));
    }

    // ─── GetPatientConversationsAsync ────────────────────────────────────────

    [Test]
    public async Task GetPatientConversations_ReturnsConversationList()
    {
        var conversations = new List<Conversation>
        {
            new Conversation { Id = "c1", PatientUserId = "patient1", Title = "Session 1" },
            new Conversation { Id = "c2", PatientUserId = "patient1", Title = "Session 2" }
        };

        _conversationRepositoryMock
            .Setup(x => x.GetByPatientIdAsync("patient1", false, default))
            .ReturnsAsync(conversations);

        var result = await _service.GetPatientConversationsAsync("patient1");

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo("c1"));
        Assert.That(result[1].Id, Is.EqualTo("c2"));
    }

    [Test]
    public async Task GetPatientConversations_EmptyList_ReturnsEmpty()
    {
        _conversationRepositoryMock
            .Setup(x => x.GetByPatientIdAsync("patient1", false, default))
            .ReturnsAsync(new List<Conversation>());

        var result = await _service.GetPatientConversationsAsync("patient1");

        Assert.That(result, Is.Empty);
    }

    // ─── GetConversationAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetConversation_Exists_ReturnsResponse()
    {
        var conversation = new Conversation
        {
            Id = "conv1",
            PatientUserId = "patient1",
            Title = "Test",
            IsArchived = false
        };

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(conversation);

        var result = await _service.GetConversationAsync("conv1");

        Assert.That(result.Id, Is.EqualTo("conv1"));
        Assert.That(result.PatientId, Is.EqualTo("patient1"));
    }

    [Test]
    public void GetConversation_NotFound_ThrowsKeyNotFoundException()
    {
        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("missing", default))
            .ReturnsAsync((Conversation?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetConversationAsync("missing"));
    }

    // ─── UpdateConversationAsync ─────────────────────────────────────────────

    [Test]
    public async Task UpdateConversation_Exists_UpdatesIsArchived()
    {
        var conversation = new Conversation { Id = "conv1", IsArchived = false };

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(conversation);
        _conversationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Conversation>(), default))
            .Returns(Task.CompletedTask);

        await _service.UpdateConversationAsync("conv1", isArchived: true);

        _conversationRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<Conversation>(c => c.IsArchived == true), default),
            Times.Once);
    }

    [Test]
    public void UpdateConversation_NotFound_ThrowsKeyNotFoundException()
    {
        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("missing", default))
            .ReturnsAsync((Conversation?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateConversationAsync("missing", true));
    }

    // ─── SendMessageAsync ────────────────────────────────────────────────────

    private void SetupSendMessageHappyPath(string patientId, string conversationId, string aiReply)
    {
        _rateLimitingServiceMock
            .Setup(x => x.CheckAndIncrementAsync(patientId, default))
            .ReturnsAsync(new RateLimitCheckResult { AllowedToChat = true, Message = "OK" });

        _riskClassifierMock
            .Setup(x => x.ClassifyRiskAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new RiskClassificationResult { RiskLevel = RiskLevel.Normal });

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, default))
            .ReturnsAsync(new Conversation { Id = conversationId, PatientUserId = patientId, MessageCount = 0 });

        _messageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ConversationMessage>(), default))
            .ReturnsAsync(new ConversationMessage());

        _guardRailRepositoryMock
            .Setup(x => x.GetByPatientUserIdAsync(patientId, true, default))
            .ReturnsAsync(new List<GuardRail>());

        _messageRepositoryMock
            .Setup(x => x.GetByConversationIdAsync(conversationId, default))
            .ReturnsAsync(new List<ConversationMessage>());

        _claudeServiceMock
            .Setup(x => x.GetTherapyResponseAsync(It.IsAny<ChatRequest>(), It.IsAny<List<(string, string)>>(), It.IsAny<List<string>>(), default))
            .ReturnsAsync(new ChatResponse { Message = aiReply });

        _guardRailEnforcementServiceMock
            .Setup(x => x.ApplyGuardRailsAsync(It.IsAny<string>(), It.IsAny<List<GuardRail>>(), default))
            .ReturnsAsync(aiReply);

        _conversationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Conversation>(), default))
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task SendMessage_HappyPath_ReturnsAssistantResponse()
    {
        SetupSendMessageHappyPath("patient1", "conv1", "I'm here to help.");

        var result = await _service.SendMessageAsync("patient1", new SendMessageRequest
        {
            ConversationId = "conv1",
            Message = "I feel anxious"
        });

        Assert.That(result.Role, Is.EqualTo("assistant"));
        Assert.That(result.Content, Is.EqualTo("I'm here to help."));
    }

    [Test]
    public async Task SendMessage_HappyPath_IncreasesMessageCountByTwo()
    {
        SetupSendMessageHappyPath("patient1", "conv1", "AI response");

        await _service.SendMessageAsync("patient1", new SendMessageRequest
        {
            ConversationId = "conv1",
            Message = "Hello"
        });

        _conversationRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<Conversation>(c => c.MessageCount == 2), default),
            Times.Once);
    }

    [Test]
    public void SendMessage_RateLimitExceeded_ThrowsInvalidOperation()
    {
        _rateLimitingServiceMock
            .Setup(x => x.CheckAndIncrementAsync("patient1", default))
            .ReturnsAsync(new RateLimitCheckResult
            {
                AllowedToChat = false,
                Message = "Daily limit reached"
            });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendMessageAsync("patient1", new SendMessageRequest
            {
                ConversationId = "conv1",
                Message = "Hello"
            }));

        Assert.That(ex.Message, Is.EqualTo("Daily limit reached"));
    }

    [Test]
    public void SendMessage_CrisisMessage_ThrowsInvalidOperation()
    {
        _rateLimitingServiceMock
            .Setup(x => x.CheckAndIncrementAsync("patient1", default))
            .ReturnsAsync(new RateLimitCheckResult { AllowedToChat = true });

        _riskClassifierMock
            .Setup(x => x.ClassifyRiskAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new RiskClassificationResult { RiskLevel = RiskLevel.Crisis });

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendMessageAsync("patient1", new SendMessageRequest
            {
                ConversationId = "conv1",
                Message = "I want to kill myself"
            }));
    }

    [Test]
    public void SendMessage_ConversationNotFound_ThrowsKeyNotFoundException()
    {
        _rateLimitingServiceMock
            .Setup(x => x.CheckAndIncrementAsync("patient1", default))
            .ReturnsAsync(new RateLimitCheckResult { AllowedToChat = true });

        _riskClassifierMock
            .Setup(x => x.ClassifyRiskAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new RiskClassificationResult { RiskLevel = RiskLevel.Normal });

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("missing", default))
            .ReturnsAsync((Conversation?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SendMessageAsync("patient1", new SendMessageRequest
            {
                ConversationId = "missing",
                Message = "Hello"
            }));
    }

    [Test]
    public void SendMessage_ConversationBelongsToDifferentPatient_ThrowsKeyNotFoundException()
    {
        _rateLimitingServiceMock
            .Setup(x => x.CheckAndIncrementAsync("patient1", default))
            .ReturnsAsync(new RateLimitCheckResult { AllowedToChat = true });

        _riskClassifierMock
            .Setup(x => x.ClassifyRiskAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new RiskClassificationResult { RiskLevel = RiskLevel.Normal });

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync("conv1", default))
            .ReturnsAsync(new Conversation { Id = "conv1", PatientUserId = "otherpatient" });

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SendMessageAsync("patient1", new SendMessageRequest
            {
                ConversationId = "conv1",
                Message = "Hello"
            }));
    }

    // ─── GetConversationMessagesAsync ────────────────────────────────────────

    [Test]
    public async Task GetConversationMessages_ReturnsMessages()
    {
        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Id = "m1", Role = MessageRole.User, Content = "Hi", ConversationId = "conv1" },
            new ConversationMessage { Id = "m2", Role = MessageRole.Assistant, Content = "Hello", ConversationId = "conv1" }
        };

        _messageRepositoryMock
            .Setup(x => x.GetByConversationIdAsync("conv1", default))
            .ReturnsAsync(messages);

        var result = await _service.GetConversationMessagesAsync("conv1");

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Role, Is.EqualTo("user"));
        Assert.That(result[1].Role, Is.EqualTo("assistant"));
    }
}
