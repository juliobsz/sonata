using System.Text.Json.Serialization;

namespace Sonata.Server.Controllers;

public sealed class ContinueConversationRequest
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }
}