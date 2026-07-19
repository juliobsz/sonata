using System.Text.Json.Serialization;

namespace Sonata.Desktop.Models;

public sealed class ContinueConversationResponse
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }
}

public sealed class ConversationListResponse
{
    [JsonPropertyName("conversations")]
    public Conversation[] Conversations { get; set; } = [];
}

public sealed class MessageResponse
{
    [JsonPropertyName("messages")]
    public Message[] Messages { get; set; } = [];
}
