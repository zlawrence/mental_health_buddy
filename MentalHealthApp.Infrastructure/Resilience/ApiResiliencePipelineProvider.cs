using System.Collections.Concurrent;
using MentalHealthApp.Application.Services;
using Polly;

namespace MentalHealthApp.Infrastructure.Resilience;

/// <summary>
/// Singleton that owns a circuit-breaker pipeline per named API context.
/// Pipelines must live as long as the process so circuit-breaker state is shared across requests.
/// </summary>
public sealed class ApiResiliencePipelineProvider
{
    private readonly IResilienceAuditLogger _auditLogger;
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new();

    public ApiResiliencePipelineProvider(IResilienceAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public ResiliencePipeline GetPipeline(string context)
        => _pipelines.GetOrAdd(context, ctx => ResiliencePolicies.BuildApiPipeline(_auditLogger, ctx));
}
