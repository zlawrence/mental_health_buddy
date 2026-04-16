using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IEmergencyContactRepository : IRepository<EmergencyContact>
{
    Task<List<EmergencyContact>> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
    Task<EmergencyContact?> GetPrimaryByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
    Task DeleteByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
}
