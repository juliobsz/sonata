using System.ComponentModel.DataAnnotations.Schema;

namespace Sonata.Server.Models;

[Table("conversations")]
public sealed class Conversation
{
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    [Column("ended_at")]
    public DateTimeOffset? EndedAt { get; set; }
    
    public ICollection<Message> Messages { get; } = new List<Message>();
}
