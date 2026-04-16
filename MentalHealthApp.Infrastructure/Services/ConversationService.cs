using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Fx;

namespace MentalHealthApp.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly IGuardRailRepository _guardRailRepository;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IRiskClassifier _riskClassifier;
    private readonly ILLMTherapyService _claudeService;
    private readonly IGuardRailEnforcementService _guardRailEnforcementService;

    public ConversationService(
        IConversationRepository conversationRepository,
        IConversationMessageRepository messageRepository,
        IGuardRailRepository guardRailRepository,
        IRateLimitingService rateLimitingService,
        IRiskClassifier riskClassifier,
        ILLMTherapyService claudeService,
        IGuardRailEnforcementService guardRailEnforcementService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _guardRailRepository = guardRailRepository;
        _rateLimitingService = rateLimitingService;
        _riskClassifier = riskClassifier;
        _claudeService = claudeService;
        _guardRailEnforcementService = guardRailEnforcementService;
    }

    public async Task<ConversationResponse> CreateConversationAsync(string patientId, CreateConversationRequest request, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation
        {
            PatientUserId = patientId,
            Title = request.Title,
            StartedDate = DateTime.UtcNow,
            IsArchived = false,
            MessageCount = 0
        };

        var created = await _conversationRepository.AddAsync(conversation, cancellationToken);

        return new ConversationResponse
        {
            Id = created.Id,
            PatientId = created.PatientUserId,
            Title = created.Title,
            StartedDate = created.StartedDate,
            IsArchived = created.IsArchived,
            MessageCount = created.MessageCount,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<List<ConversationResponse>> GetPatientConversationsAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var conversations = await _conversationRepository.GetByPatientIdAsync(patientId, includeArchived: false, cancellationToken);
        
        return conversations.Select(c => new ConversationResponse
        {
            Id = c.Id,
            PatientId = c.PatientUserId,
            Title = c.Title,
            StartedDate = c.StartedDate,
            IsArchived = c.IsArchived,
            MessageCount = c.MessageCount,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<ConversationResponse> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found");
        }

        return new ConversationResponse
        {
            Id = conversation.Id,
            PatientId = conversation.PatientUserId,
            Title = conversation.Title,
            StartedDate = conversation.StartedDate,
            IsArchived = conversation.IsArchived,
            MessageCount = conversation.MessageCount,
            CreatedAt = conversation.CreatedAt
        };
    }

    public async Task UpdateConversationAsync(string conversationId, bool isArchived, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found");
        }

        conversation.IsArchived = isArchived;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversationRepository.UpdateAsync(conversation, cancellationToken);
    }

    public async Task<MessageResponse> SendMessageAsync(string patientId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        // Check rate limit
        var rateLimitCheck = await _rateLimitingService.CheckAndIncrementAsync(patientId, cancellationToken);
        if (!rateLimitCheck.AllowedToChat)
        {
            throw new InvalidOperationException(rateLimitCheck.Message);
        }

        // Check for risk level
        var riskResult = await _riskClassifier.ClassifyRiskAsync(request.Message, cancellationToken);
        if (riskResult.RiskLevel == RiskLevel.Crisis)
        {
            // TODO: Trigger emergency alert notification
            throw new InvalidOperationException("Your message requires immediate attention from a professional. Emergency contacts have been notified.");
        }

        // Get conversation
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation == null || conversation.PatientUserId != patientId)
        {
            throw new KeyNotFoundException("Conversation not found");
        }

        // Save user message
        var userMessage = new ConversationMessage
        {
            ConversationId = request.ConversationId,
            Role = MessageRole.User,
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(userMessage, cancellationToken);

        // Get guard rails
        var guardRails = await _guardRailRepository.GetByPatientUserIdAsync(patientId, activeOnly: true, cancellationToken);

        // Get conversation history
        var history = await _messageRepository.GetByConversationIdAsync(request.ConversationId, cancellationToken);
        var conversationHistory = history.Select(m => (m.Role.ToString().ToLower(), m.Content)).ToList();

        // Build guard rail instructions
        var guardRailInstructions = guardRails.Select(g => 
            $"{(g.Action == Domain.Entities.GuardRailAction.Remove ? "Remove" : "Replace")} the keyword '{g.Keyword}'{(string.IsNullOrEmpty(g.Replacement) ? "" : $" with '{g.Replacement}'")}"
        ).ToList();

        // Call Claude API
        var claudeResponse = await _claudeService.GetTherapyResponseAsync(
            new ChatRequest
            {
                UserId = patientId,
                Message = request.Message
            },
            conversationHistory,
            guardRailInstructions,
            cancellationToken);

        // Apply guard rails to Claude response
        var enforcedResponse = await _guardRailEnforcementService.ApplyGuardRailsAsync(claudeResponse.Message, guardRails, cancellationToken);

        // Save assistant message
        var assistantMessage = new ConversationMessage
        {
            ConversationId = request.ConversationId,
            Role = MessageRole.Assistant,
            Content = enforcedResponse,
            Timestamp = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(assistantMessage, cancellationToken);

        // Update conversation message count
        conversation.MessageCount += 2;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversationRepository.UpdateAsync(conversation, cancellationToken);

        return new MessageResponse
        {
            Id = assistantMessage.Id,
            ConversationId = assistantMessage.ConversationId,
            Role = assistantMessage.Role.ToString().ToLower(),
            Content = assistantMessage.Content,
            Timestamp = assistantMessage.Timestamp
        };
    }

    public async Task<List<MessageResponse>> GetConversationMessagesAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var messages = await _messageRepository.GetByConversationIdAsync(conversationId, cancellationToken);

        return messages.Select(m => new MessageResponse
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role.ToString().ToLower(),
            Content = m.Content,
            Timestamp = m.Timestamp
        }).ToList();
    }
}
