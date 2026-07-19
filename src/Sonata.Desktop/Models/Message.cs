namespace Sonata.Desktop.Models;

public sealed class Message
{
    public long Id { get; set; }
    
    public Guid ConversationId  { get; set; }
    
    public int Sequence { get; set; }
    
    public string Content { get; set; } = null!;
    
    public string Role { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
}
