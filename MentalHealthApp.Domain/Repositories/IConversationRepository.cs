using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<List<Conversation>> GetByPatientIdAsync(string patientId, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<List<Conversation>> SearchByPatientAndKeywordAsync(string patientId, string keyword, CancellationToken cancellationToken = default);
    Task<Conversation?> GetLatestByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
}
