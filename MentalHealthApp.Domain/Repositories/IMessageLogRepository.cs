using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IMessageLogRepository : IRepository<MessageLog>
{
    Task<List<MessageLog>> GetByStatusAsync(MessageLogStatus status, CancellationToken cancellationToken = default);
    Task<List<MessageLog>> GetByTypeAsync(MessageType messageType, CancellationToken cancellationToken = default);
}
