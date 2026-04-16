using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface ITherapistAccessRepository : IRepository<TherapistAccess>
{
    Task<List<TherapistAccess>> GetByPatientIdAsync(string patientId, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<List<TherapistAccess>> GetByTherapistIdAsync(string therapistId, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<TherapistAccess?> GetByTherapistAndPatientAsync(string therapistId, string patientId, CancellationToken cancellationToken = default);
}
