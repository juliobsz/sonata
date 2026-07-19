using Sonata.Server.ModelProviders;

namespace Sonata.Server.Tests.ModelProviders;

public sealed class ScriptedModelProvider(GeneratedResponse response) : IModelProvider
{
    public GenerateResponseRequest? ReceivedRequest { get; private set; }

    public Task<GeneratedResponse> GenerateResponseAsync(GenerateResponseRequest request,
        CancellationToken cancellationToken)
    {
        ReceivedRequest = request;
        return Task.FromResult(response);
    }

    public sealed class FailingModelProvider : IModelProvider
    {
        public Task<GeneratedResponse> GenerateResponseAsync(GenerateResponseRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromException<GeneratedResponse>(new ModelProviderException("Scripted provider failure."));
        }
    }
}

