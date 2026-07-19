using System.Text.Json.Serialization;

namespace Sonata.Desktop.Models;

public sealed class MemoryDiffItem
{
    [JsonPropertyName("memoryId")]
    public Guid MemoryId { get; set; }
    
    [JsonPropertyName("sourceNoteId")]
    public Guid SourceNoteId { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("rank")]
    public int Rank { get; set; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}