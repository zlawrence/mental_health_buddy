using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MentalHealthApp.Infrastructure.Services;

public sealed class EmailQueueProcessor : BackgroundService
{
    private readonly EmailQueueChannel _queue;
    private readonly IThirdPartyEmailService _emailService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueProcessor> _logger;

    public EmailQueueProcessor(
        EmailQueueChannel queue,
        IThirdPartyEmailService emailService,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailQueueProcessor> logger)
    {
        _queue = queue;
        _emailService = emailService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email queue processor started");

        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _emailService.SendAsync(message, stoppingToken);
                await UpdateLogAsync(message.LogId, MessageLogStatus.Sent, sentAt: DateTime.UtcNow, failureMessage: null, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver email to {To}: {Subject}", message.To, message.Subject);
                await UpdateLogAsync(message.LogId, MessageLogStatus.Failed, sentAt: null, failureMessage: ex.Message, stoppingToken);
            }
        }

        _logger.LogInformation("Email queue processor stopped");
    }

    private async Task UpdateLogAsync(string? logId, MessageLogStatus status, DateTime? sentAt, string? failureMessage, CancellationToken cancellationToken)
    {
        if (logId is null) return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IMessageLogRepository>();

            var log = await repo.GetByIdAsync(logId, cancellationToken);
            if (log is null) return;

            log.Status = status;
            log.FailureMessage = failureMessage;
            log.SentAt = sentAt;
            log.UpdatedAt = DateTime.UtcNow;

            await repo.UpdateAsync(log, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update message log {LogId}", logId);
        }
    }
}
