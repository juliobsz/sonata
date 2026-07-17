namespace Sonata.Server.ModelProviders;

public interface IModelProvider
{
    Task<GeneratedResponse> GenerateResponseAsync(GenerateResponseRequest request, CancellationToken cancellationToken);
}