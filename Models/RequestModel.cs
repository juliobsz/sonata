namespace qwen_hackathon_api.Models;

public class ChatRequest
{
    public string? Message { get; set; } = string.Empty;
    public string? Session { get; set; } =  string.Empty;
}