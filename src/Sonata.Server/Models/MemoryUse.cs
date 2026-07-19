using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sonata.Server.Models;

[Table("memory_uses")]
public sealed class MemoryUse
{
    [Column("id")]
    public long Id { get; set; }
    
    [Column("memory_id")]
    public Guid MemoryId { get; set; }

    public Memory Memory { get; set; } = null!;
    
    [Column("response_message_id")]
    public long ResponseMessageId { get; set; }

    public Message ResponseMessage { get; set; } = null!;
    
    [Column("rank")]
    public int Rank { get; set; }
    
    [Column("reason")]
    [MaxLength(50)]
    public string Reason { get; set; } = null!;
    
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}