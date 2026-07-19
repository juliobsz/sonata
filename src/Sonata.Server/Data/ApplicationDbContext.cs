using Microsoft.EntityFrameworkCore;
using Sonata.Server.Models;

namespace Sonata.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>()
            .HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Message>().HasIndex(message => new
        {
            message.ConversationId,
            message.Sequence
        }).IsUnique();
        
        modelBuilder.Entity<Message>().ToTable("messages", table => 
            table.HasCheckConstraint("CK_messages_sequence_positive", "sequence > 0"));
    }
}
