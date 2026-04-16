using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface ITherapistInvitationRepository : IRepository<TherapistInvitation>
{
    Task<TherapistInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<TherapistInvitation>> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
    Task<TherapistInvitation?> GetActiveByPatientAndEmailAsync(string patientId, string email, CancellationToken cancellationToken = default);
}
