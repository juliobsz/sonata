using Sonata.Server.Models;

namespace Sonata.Server.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetMessageAsync(long id);
    Task<Message> AddMessageAsync(Message message);
    Task<IReadOnlyList<Message>> GetMessagesByConversationId(Guid conversationId);
}