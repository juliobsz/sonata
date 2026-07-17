using System.Text.Json.Serialization;

namespace Sonata.Server.ModelProviders.Qwen;

internal sealed class QwenResponseRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }
    
    [JsonPropertyName("input")]
    public required IReadOnlyList<QwenInputMessage> Input { get; init; }
}

internal sealed class QwenInputMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }
    
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

internal sealed class QwenResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("output")]
    public IReadOnlyList<QwenOutput> Output { get; init; } = [];
}

internal sealed class QwenOutput
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = "assistant";
    
    [JsonPropertyName("content")]
    public IReadOnlyList<QwenOutputContent> Content { get; init; } = [];
}

internal sealed class QwenOutputContent
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}



