using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IGuardRailRepository : IRepository<GuardRail>
{
    Task<List<GuardRail>> GetByPatientUserIdAsync(string patientId, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<List<GuardRail>> GetByPatientAndTherapistAsync(string patientId, string therapistId, CancellationToken cancellationToken = default);
    Task DeleteByPatientUserIdAsync(string patientId, CancellationToken cancellationToken = default);
}
