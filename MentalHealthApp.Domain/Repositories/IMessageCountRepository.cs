using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IMessageCountRepository : IRepository<MessageCount>
{
    Task<MessageCount?> GetByPatientAndDateAsync(string patientId, DateTime date, CancellationToken cancellationToken = default);
    Task<MessageCount> IncrementCountAsync(string patientId, DateTime date, CancellationToken cancellationToken = default);
}
