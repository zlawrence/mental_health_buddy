using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MentalHealthApp.Infrastructure.Resilience;

public sealed class ResilienceAuditLogger : IResilienceAuditLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ResilienceAuditLogger> _logger;

    public ResilienceAuditLogger(IServiceScopeFactory scopeFactory, ILogger<ResilienceAuditLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(string action, string details, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            await repo.AddAsync(new AuditLog
            {
                AdminId = "system",
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write resilience audit log for action {Action}", action);
        }
    }
}
