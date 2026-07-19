using Microsoft.EntityFrameworkCore;
using Sonata.Server.Data;
using Sonata.Server.Models;

namespace Sonata.Server.Repositories;

public sealed class ConversationRepository(ApplicationDbContext context) : IConversationRepository
{
    public async Task<Conversation?> GetConversationAsync(Guid id)
    {
        return await context.Conversations.FindAsync(id);
    }

    public async Task<Conversation> AddConversationAsync(Conversation conversation)
    {
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return conversation;
    }

    public async Task<IEnumerable<Conversation>> GetConversationsAsync()
    {
        return await context.Conversations.ToArrayAsync();
    }
}
