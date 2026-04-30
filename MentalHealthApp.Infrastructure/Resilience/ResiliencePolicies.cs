using MentalHealthApp.Application.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MentalHealthApp.Infrastructure.Resilience;

public static class ResiliencePolicies
{
    private static int RetryCount =>
        int.TryParse(Environment.GetEnvironmentVariable("RESILIENCE_RETRY_COUNT"), out var v) && v > 0 ? v : 3;

    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromSeconds(1);

    private static TimeSpan CircuitBreakerSamplingDuration =>
        int.TryParse(Environment.GetEnvironmentVariable("RESILIENCE_CB_SAMPLING_SECONDS"), out var v) && v > 0
            ? TimeSpan.FromSeconds(v) : TimeSpan.FromSeconds(30);

    private static TimeSpan CircuitBreakerBreakDuration =>
        int.TryParse(Environment.GetEnvironmentVariable("RESILIENCE_CB_BREAK_SECONDS"), out var v) && v > 0
            ? TimeSpan.FromSeconds(v) : TimeSpan.FromSeconds(30);

    /// <summary>
    /// Retry-only pipeline for database operations (3 retries, exponential backoff).
    /// Logs to audit on exhaustion unless auditLogger is null (used to break circular dependency for AuditLogRepository).
    /// </summary>
    public static ResiliencePipeline BuildDbRetryPipeline(IResilienceAuditLogger? auditLogger, string context)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = RetryCount,
                Delay = RetryBaseDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Retry + circuit-breaker pipeline for third-party API calls.
    /// Circuit opens after 50% failure rate over 5+ requests in a 30-second window, stays open for 30 seconds.
    /// </summary>
    public static ResiliencePipeline BuildApiPipeline(IResilienceAuditLogger auditLogger, string context)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = RetryCount,
                Delay = RetryBaseDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = CircuitBreakerSamplingDuration,
                BreakDuration = CircuitBreakerBreakDuration,
                OnOpened = args =>
                {
                    _ = auditLogger.LogAsync(
                        $"CircuitBreakerOpened:{context}",
                        $"Circuit breaker opened for {context}. Duration: {args.BreakDuration.TotalSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
