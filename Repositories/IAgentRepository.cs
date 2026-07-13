using qwen_hackathon_api.Models;

namespace qwen_hackathon_api.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetSession(int id);
    Task<Session> AddSession(Session session);
}

public interface IMessageRepository
{
    Task<Message?> GetMessage(int id);
    Task<Message> AddMessage(Message message);
    Task<IEnumerable<Message>> GetMessagesBySessionId(Guid sessionId);
}