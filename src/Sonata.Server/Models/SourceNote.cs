using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sonata.Server.Models;

[Table("source_notes")]
public sealed class SourceNote
{
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("message_id")]
    public long MessageId { get; set; }

    public Message Message { get; set; } = null!;
    
    [Column("excerpt")]
    [MaxLength(500)]
    public string Excerpt { get; set; } = null!;
    
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public Memory? Memory { get; set; }
}