using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public class RateLimitingService : IRateLimitingService
{
    private const int MaxMessagesPerDay = 20;
    private const int WarningThreshold = 18;

    private readonly IMessageCountRepository _messageCountRepository;

    public RateLimitingService(IMessageCountRepository messageCountRepository)
    {
        _messageCountRepository = messageCountRepository;
    }

    public async Task<RateLimitCheckResult> CheckAndIncrementAsync(string patientId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var messageCount = await _messageCountRepository.IncrementCountAsync(patientId, today, cancellationToken);

        if (messageCount.Count > MaxMessagesPerDay)
        {
            return new RateLimitCheckResult
            {
                AllowedToChat = false,
                CurrentCount = messageCount.Count,
                MaxAllowed = MaxMessagesPerDay,
                Message = "You've reached your daily message limit. Please try again tomorrow."
            };
        }

        if (messageCount.Count >= WarningThreshold)
        {
            return new RateLimitCheckResult
            {
                AllowedToChat = true,
                CurrentCount = messageCount.Count,
                MaxAllowed = MaxMessagesPerDay,
                Message = $"You're approaching your daily limit ({messageCount.Count}/{MaxMessagesPerDay}). Take a break!"
            };
        }

        return new RateLimitCheckResult
        {
            AllowedToChat = true,
            CurrentCount = messageCount.Count,
            MaxAllowed = MaxMessagesPerDay,
            Message = "OK"
        };
    }
}
