using MentalHealthApp.Application.DTOs;

namespace MentalHealthApp.Application.Services;

public interface IThirdPartyEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
