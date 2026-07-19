using Microsoft.EntityFrameworkCore;
using Renci.SshNet.Messages.Transport;
using Sonata.Server.Models;

namespace Sonata.Server.Tests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class MemoryPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PersistsSourcedMemoryAndItsRecordedUse()
    {
        var conversationId = Guid.NewGuid();
        var memoryId = Guid.NewGuid();

        await using (var context = fixture.CreateDbContext())
        {
            var movement = await context.Movements.SingleAsync(
                movement => movement.Id == Movement.HackathonId);

            Assert.Equal(Movement.HackathonId, movement.Id);
            Assert.Equal("Qwen AI Hackathon", movement.Name);

            var conversation = new Conversation
            {
                Id = conversationId,
            };

            var sourceMessage = NewMessage(
                conversationId,
                sequence: 1,
                role: "user",
                content: "Use C# for the backend.");

            var responseMessage = NewMessage(
                conversationId,
                sequence: 2,
                role: "assistant",
                content: "I will keep the backend in C#.");
            
            context.AddRange(conversation, sourceMessage, responseMessage);
            await context.SaveChangesAsync();

            var sourceNote = new SourceNote
            {
                MessageId = sourceMessage.Id,
                Excerpt = sourceMessage.Content
            };

            var memory = new Memory
            {
                Id = memoryId,
                MovementId = Movement.HackathonId,
                SourceNote = sourceNote,
                Text = "The backend uses C#.",
                Type = MemoryType.ProjectContext,
                LifecycleState = MemoryLifecycleState.Active
            };

            var memoryUse = new MemoryUse
            {
                Memory = memory,
                ResponseMessageId = responseMessage.Id,
                Rank = 1,
                Reason = "MovementMatch"
            };

            context.Add(memoryUse);
            await context.SaveChangesAsync();
        }
        
        await using (var verificationContext = fixture.CreateDbContext())
        {
            var savedMemory = await verificationContext.Memories
                .AsNoTracking()
                .Include(memory => memory.Movement)
                .Include(memory => memory.SourceNote)
                .ThenInclude(sourceNote => sourceNote.Message)
                .Include(memory => memory.Uses)
                .ThenInclude(memoryUse => memoryUse.ResponseMessage)
                .SingleAsync(memory => memory.Id == memoryId);
            
            Assert.Equal("Qwen AI Hackathon", savedMemory.Movement.Name);
            Assert.Equal("The backend uses C#.", savedMemory.Text);
            Assert.Equal(MemoryType.ProjectContext, savedMemory.Type);
            Assert.Equal(MemoryLifecycleState.Active, savedMemory.LifecycleState);
            Assert.Equal("Use C# for the backend.", savedMemory.SourceNote.Excerpt);
            Assert.Equal("user", savedMemory.SourceNote.Message.Role);

            var savedUse = Assert.Single(savedMemory.Uses);
            Assert.Equal(1, savedUse.Rank);
            Assert.Equal("MovementMatch", savedUse.Reason);
            Assert.Equal("assistant", savedUse.ResponseMessage.Role);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsMemoryWithoutARealSourceNote()
    {
        await using var context = fixture.CreateDbContext();
        context.Memories.Add(new Memory
        {
            MovementId = Movement.HackathonId,
            SourceNoteId = Guid.NewGuid(),
            Text = "This claim has no evidence.",
            Type = MemoryType.ProjectContext,
            LifecycleState = MemoryLifecycleState.Active
        });
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static Message NewMessage(Guid conversationId, int sequence, string role, string content)
    {
        return new Message
        {
            ConversationId = conversationId,
            Sequence = sequence,
            Role = role,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}