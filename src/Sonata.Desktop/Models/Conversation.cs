namespace Sonata.Desktop.Models;

public sealed class Conversation
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
