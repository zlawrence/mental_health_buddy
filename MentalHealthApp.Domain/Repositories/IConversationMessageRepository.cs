using MentalHealthApp.Domain.Entities;

namespace MentalHealthApp.Domain.Repositories;

public interface IConversationMessageRepository : IRepository<ConversationMessage>
{
    Task<List<ConversationMessage>> GetByConversationIdAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<List<ConversationMessage>> GetPagedByConversationIdAsync(string conversationId, int skip, int take, CancellationToken cancellationToken = default);
    Task DeleteByConversationIdAsync(string conversationId, CancellationToken cancellationToken = default);
}
