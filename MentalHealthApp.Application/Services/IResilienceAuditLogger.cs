namespace MentalHealthApp.Application.Services;

public interface IResilienceAuditLogger
{
    Task LogAsync(string action, string details, CancellationToken cancellationToken = default);
}
