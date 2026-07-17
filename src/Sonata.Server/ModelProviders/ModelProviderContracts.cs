namespace Sonata.Server.ModelProviders;

public sealed record ModelMessage(string Role, string Content);

public sealed record GenerateResponseRequest(IReadOnlyList<ModelMessage> Messages);

public sealed record GeneratedResponse(string Text, string Role, string? ProviderResponseId);