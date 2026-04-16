using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Application.Services;

public interface IGuardRailEnforcementService
{
    Task<string> ApplyGuardRailsAsync(string content, List<GuardRail> guardRails, CancellationToken cancellationToken = default);
}
