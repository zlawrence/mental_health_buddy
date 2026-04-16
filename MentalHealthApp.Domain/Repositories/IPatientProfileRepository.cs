using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IPatientProfileRepository : IRepository<PatientProfile>
{
    Task<PatientProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
