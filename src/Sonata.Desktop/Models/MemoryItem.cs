using System.Text.Json.Serialization;

namespace Sonata.Desktop.Models;

public sealed class MemoryItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("movementId")]
    public Guid MovementId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("lifecycleState")]
    public string LifecycleState { get; set; } = string.Empty;

    [JsonPropertyName("sourceNote")]
    public SourceNoteItem SourceNote { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonIgnore]
    public bool IsActive => string.Equals(LifecycleState, "Active", StringComparison.OrdinalIgnoreCase);
}

public sealed class SourceNoteItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("messageId")]
    public long MessageId { get; set; }
    
    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = string.Empty;
    
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}