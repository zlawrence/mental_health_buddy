namespace MentalHealthApp.Application.Services;

public interface IRateLimitingService
{
    Task<RateLimitCheckResult> CheckAndIncrementAsync(string patientId, CancellationToken cancellationToken = default);
}
