using Microsoft.EntityFrameworkCore;
using Sonata.Server.Repositories;
using Sonata.Server.Models;

namespace Sonata.Server.Tests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class MessageRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddsAndReturnsMessagesInSequenceOrder()
    {
        await using var context = fixture.CreateDbContext();
        var conversation = new Conversation { Id = Guid.NewGuid() };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var repository = new MessageRepository(context);

        await repository.AddMessageAsync(NewMessage(conversation.Id, "first"));
        await repository.AddMessageAsync(NewMessage(conversation.Id, "second"));
        await repository.AddMessageAsync(NewMessage(conversation.Id, "third"));
        
        var messages = await repository.GetMessagesByConversationId(conversation.Id);
        
        Assert.Equal(new[] { 1, 2, 3 }, messages.Select(message => message.Sequence));
        Assert.Equal(new[] { "first", "second", "third" }, messages.Select(message => message.Content));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsDuplicateSequenceWithinOneConversation()
    {
        await using var context = fixture.CreateDbContext();
        var conversation = new Conversation { Id = Guid.NewGuid() };
        
        context.Add(conversation);
        context.AddRange(
            NewMessage(conversation.Id, "one", sequence: 1),
            NewMessage(conversation.Id, "duplicate", sequence: 1));
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsMessageWithoutARealConversation()
    {
        await using var context = fixture.CreateDbContext();
        context.Add(NewMessage(Guid.NewGuid(), "Orphan", sequence: 1));
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static Message NewMessage(Guid conversationId, string content, int sequence = 0)
    {
        return new Message
        {
            ConversationId = conversationId,
            Content = content,
            Role = "user",
            Sequence = sequence,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}