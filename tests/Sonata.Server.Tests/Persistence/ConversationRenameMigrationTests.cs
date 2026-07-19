using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sonata.Server.Data;
using Testcontainers.PostgreSql;

namespace Sonata.Server.Tests.Persistence;

public sealed class ConversationRenameMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18.4-alpine")
            .WithDatabase("sonata_migration_tests")
            .WithUsername("sonata")
            .WithPassword("sonata-tests-only")
            .Build();

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PreservesExistingConversationAndMessage()
    {
        await using var context = CreateDbContext();
        var migrator = context.Database.GetService<IMigrator>();

        await migrator.MigrateAsync("20260718202538_AddMessageSequence");

        var conversationId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO sessions (id, started_at)
            VALUES ({conversationId}, {createdAt});
            """);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO messages
                (session_id, sequence, content, role, created_at)
            VALUES
                ({conversationId}, 1, {"preserve me"},
                 {"user"}, {createdAt});
            """);

        await context.Database.MigrateAsync();

        var conversation = await context.Conversations.SingleAsync(item => item.Id == conversationId);
        var message = await context.Messages.SingleAsync(item =>
                item.ConversationId == conversationId);

        Assert.Equal(conversationId, conversation.Id);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal("preserve me", message.Content);
        Assert.Equal(1, message.Sequence);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;

        return new ApplicationDbContext(options);
    }
}