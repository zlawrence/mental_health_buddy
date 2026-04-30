using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;

namespace MentalHealthApp.Infrastructure.Services;

public sealed class PersistingEmailQueue : IEmailQueue
{
    private readonly EmailQueueChannel _channel;
    private readonly IMessageLogRepository _messageLogRepository;

    public PersistingEmailQueue(EmailQueueChannel channel, IMessageLogRepository messageLogRepository)
    {
        _channel = channel;
        _messageLogRepository = messageLogRepository;
    }

    public async ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var log = new MessageLog
        {
            MessageType = MessageType.Email,
            To = message.To,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
            Status = MessageLogStatus.Queued
        };

        await _messageLogRepository.AddAsync(log, cancellationToken);

        message.LogId = log.Id;

        await _channel.EnqueueAsync(message, cancellationToken);
    }
}
