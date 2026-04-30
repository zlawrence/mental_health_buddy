using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
