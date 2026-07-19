using Microsoft.EntityFrameworkCore;
using Sonata.Server.Memories;
using Sonata.Server.Models;

namespace Sonata.Server.Tests.Memories;

[Collection(Persistence.PostgreSqlCollection.Name)]
public sealed class MemoryServiceTests(
    Persistence.PostgreSqlFixture fixture)
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreatesAndListsMemoryFromAUserMessage()
    {
        var sourceMessageId = await AddMessageAsync(
            Movement.HackathonId,
            role: "user",
            content: "Use C# for the backend.");

        var memoryId = Guid.Empty;

        await using (var context = fixture.CreateDbContext())
        {
            IMemoryService service = new MemoryService(context);

            var result = await service.CreateAsync(
                new CreateMemoryCommand(
                    sourceMessageId,
                    Movement.HackathonId,
                    "  The backend uses C#.  ",
                    MemoryType.ProjectContext));

            Assert.True(result.Succeeded);
            Assert.Equal(MemoryError.None, result.Error);
            Assert.NotNull(result.Memory);
            Assert.Equal("The backend uses C#.", result.Memory.Text);
            Assert.Equal(
                MemoryLifecycleState.Active,
                result.Memory.LifecycleState);
            Assert.Equal(
                sourceMessageId,
                result.Memory.SourceNote.MessageId);
            Assert.Equal(
                "Use C# for the backend.",
                result.Memory.SourceNote.Excerpt);

            memoryId = result.Memory.Id;
        }

        await using (var verificationContext = fixture.CreateDbContext())
        {
            IMemoryService service = new MemoryService(verificationContext);
            var memories = await service.ListAsync(Movement.HackathonId);

            var savedMemory = Assert.Single(
                memories,
                memory => memory.Id == memoryId);
            Assert.Equal(MemoryType.ProjectContext, savedMemory.Type);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsAnAssistantMessageAsEvidence()
    {
        var sourceMessageId = await AddMessageAsync(
            Movement.HackathonId,
            role: "assistant",
            content: "Generated text is not a user decision.");

        await using var context = fixture.CreateDbContext();
        IMemoryService service = new MemoryService(context);

        var result = await service.CreateAsync(
            new CreateMemoryCommand(
                sourceMessageId,
                Movement.HackathonId,
                "The backend uses C#.",
                MemoryType.ProjectContext));

        Assert.False(result.Succeeded);
        Assert.Equal(
            MemoryError.SourceMessageMustBeUser,
            result.Error);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsAnUnknownSourceMessage()
    {
        await using var context = fixture.CreateDbContext();
        IMemoryService service = new MemoryService(context);

        var result = await service.CreateAsync(
            new CreateMemoryCommand(
                long.MaxValue,
                Movement.HackathonId,
                "This has no source.",
                MemoryType.ProjectContext));

        Assert.False(result.Succeeded);
        Assert.Equal(
            MemoryError.SourceMessageNotFound,
            result.Error);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsAnUnsupportedMemoryType()
    {
        var sourceMessageId = await AddMessageAsync(
            Movement.HackathonId,
            role: "user",
            content: "There is evidence for a supported type.");

        await using var context = fixture.CreateDbContext();
        IMemoryService service = new MemoryService(context);

        var result = await service.CreateAsync(
            new CreateMemoryCommand(
                sourceMessageId,
                Movement.HackathonId,
                "This type is invalid.",
                (MemoryType)999));

        Assert.False(result.Succeeded);
        Assert.Equal(MemoryError.UnsupportedType, result.Error);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsEvidenceFromAnotherMovement()
    {
        var otherMovementId = Guid.NewGuid();
        var sourceMessageId = await AddMessageAsync(
            otherMovementId,
            role: "user",
            content: "This belongs elsewhere.",
            movementName: "Other Movement");

        await using var context = fixture.CreateDbContext();
        IMemoryService service = new MemoryService(context);

        var result = await service.CreateAsync(
            new CreateMemoryCommand(
                sourceMessageId,
                Movement.HackathonId,
                "This must not cross Movements.",
                MemoryType.Decision));

        Assert.False(result.Succeeded);
        Assert.Equal(
            MemoryError.SourceMessageOutsideMovement,
            result.Error);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectsBlankMemoryText()
    {
        var sourceMessageId = await AddMessageAsync(
            Movement.HackathonId,
            role: "user",
            content: "There is real evidence here.");

        await using var context = fixture.CreateDbContext();
        IMemoryService service = new MemoryService(context);

        var result = await service.CreateAsync(
            new CreateMemoryCommand(
                sourceMessageId,
                Movement.HackathonId,
                "   ",
                MemoryType.Decision));

        Assert.False(result.Succeeded);
        Assert.Equal(MemoryError.InvalidText, result.Error);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ArchivesMemoryIdempotently()
    {
        var sourceMessageId = await AddMessageAsync(
            Movement.HackathonId,
            role: "user",
            content: "Archive this synthetic preference.");

        await using var context = fixture.CreateDbContext();
        IMemoryService service = new MemoryService(context);
        var created = await service.CreateAsync(
            new CreateMemoryCommand(
                sourceMessageId,
                Movement.HackathonId,
                "Prefer concise guidance.",
                MemoryType.Preference));

        Assert.True(created.Succeeded);
        Assert.NotNull(created.Memory);

        var firstArchive = await service.ArchiveAsync(created.Memory.Id);
        var secondArchive = await service.ArchiveAsync(created.Memory.Id);

        Assert.True(firstArchive.Succeeded);
        Assert.True(secondArchive.Succeeded);
        Assert.Equal(
            MemoryLifecycleState.Archived,
            secondArchive.Memory?.LifecycleState);
    }

    private async Task<long> AddMessageAsync(
        Guid movementId,
        string role,
        string content,
        string? movementName = null)
    {
        await using var context = fixture.CreateDbContext();

        if (movementId != Movement.HackathonId)
        {
            context.Movements.Add(new Movement
            {
                Id = movementId,
                Name = movementName ?? "Synthetic Movement"
            });
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            MovementId = movementId
        };

        var message = new Message
        {
            Conversation = conversation,
            Sequence = 1,
            Role = role,
            Content = content
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync();
        return message.Id;
    }
}