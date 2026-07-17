using System.ComponentModel.DataAnnotations;

namespace Sonata.Server.ModelProviders.Qwen;

public sealed class QwenOptions
{
    public const string SectionName = "Qwen";
    
    [Required]
    public string Model { get; init; } = "";
    [Required]
    [Url]
    public string ApiUrl { get; init; } = "";
    [Required]
    public string ApiKey { get; init; } = "";
    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 60;
}

