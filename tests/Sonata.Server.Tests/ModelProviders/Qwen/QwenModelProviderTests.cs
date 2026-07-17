using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sonata.Server.ModelProviders;
using Sonata.Server.ModelProviders.Qwen;

namespace Sonata.Server.Tests.ModelProviders.Qwen;

public sealed class QwenModelProviderTests
{
    [Fact]
    public async Task GenerateResponseAsync_MapsRequestAndResponse()
    {
        string? capturedJson = null;
        Uri? capturedUri = null;
        string? capturedAuthorization = null;

        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            capturedUri = request.RequestUri;
            capturedAuthorization = request.Headers.Authorization?.ToString();
            capturedJson = await request.Content!.ReadAsStringAsync(cancellationToken);

            const string responseJson = """
                                        {
                                          "id": "response-123",
                                          "output": [
                                            {
                                              "role": "assistant",
                                              "content": [
                                                { "text": "Use PostgreSQL." }
                                              ]
                                            }
                                          ]
                                        }
                                        """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var provider = CreateProvider(handler);

        var providerRequest = new GenerateResponseRequest([
            new ModelMessage("user", "Which database?"),
            new ModelMessage("assistant", "Let me check."),
            new ModelMessage("user", "Use our project decision.")
        ]);
        
        var result = await provider.GenerateResponseAsync(providerRequest, CancellationToken.None);
        Assert.Equal("Use PostgreSQL.", result.Text);
        Assert.Equal("assistant", result.Role);
        Assert.Equal("response-123", result.ProviderResponseId);
        
        Assert.Equal(new Uri("https://provider.example/v1/responses"), capturedUri);
        Assert.Equal("Bearer test-key", capturedAuthorization);
        Assert.NotNull(capturedJson);
        
        using var document = JsonDocument.Parse(capturedJson);
        var root = document.RootElement;
        var input = root.GetProperty("input");
        
        Assert.Equal("qwen-test", root.GetProperty("model").GetString());
        Assert.Equal(3, input.GetArrayLength());
        Assert.Equal("Which database?", input[0].GetProperty("content").GetString());
        Assert.Equal("Use our project decision.", input[2].GetProperty("content").GetString());
    }
    
    [Fact]
    public async Task GenerateResponseAsync_WhenProviderRejects_ThrowsTypedError()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var provider = CreateProvider(handler);

        var request = new GenerateResponseRequest([
            new ModelMessage("user", "Hello")
        ]);

        var exception = await Assert.ThrowsAsync<ModelProviderException>(() => 
            provider.GenerateResponseAsync(request, CancellationToken.None));
        
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }
    
    [Fact]
    public async Task GenerateResponseAsync_WhenOutputIsMissing_ThrowsTypedError()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{ \"id\": \"response-empty\" }",
                        Encoding.UTF8,
                        "application/json")
                }));
        var provider = CreateProvider(handler);

        var request = new GenerateResponseRequest([
            new ModelMessage("user", "Hello")
        ]);

        await Assert.ThrowsAsync<ModelProviderException>(() => 
            provider.GenerateResponseAsync(request, CancellationToken.None));
    }
    
    private static QwenModelProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new QwenOptions
        {
            Model = "qwen-test",
            ApiUrl = "https://provider.example/v1",
            ApiKey = "test-key",
            TimeoutSeconds = 30
        });

        return new QwenModelProvider(httpClient, options);
    }
}