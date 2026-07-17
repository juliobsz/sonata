using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Sonata.Server.ModelProviders.Qwen;

public sealed class QwenModelProvider : IModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly QwenOptions _options;

    public QwenModelProvider(HttpClient httpClient, IOptions<QwenOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        
        _httpClient.BaseAddress = new Uri(_options.ApiUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<GeneratedResponse> GenerateResponseAsync(GenerateResponseRequest request,
        CancellationToken cancellationToken)
    {
        var qwenRequest = new QwenResponseRequest
        {
            Model = _options.Model,
            Input = request.Messages.Select(message => new QwenInputMessage
            {
                Role = message.Role,
                Content = message.Content
            }).ToArray()
        };

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("responses", qwenRequest, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ModelProviderException("The model provider request timed out.", innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new ModelProviderException("The model provider couldn't be reached.", innerException: exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode) throw new ModelProviderException("The model provider returned HTTP " + response.StatusCode, response.StatusCode);

            QwenResponse? qwenResponse;

            try
            {
                qwenResponse = await response.Content.ReadFromJsonAsync<QwenResponse>(cancellationToken: cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new ModelProviderException("The model provider returned invalid JSON.", response.StatusCode, exception);
            }

            var output = qwenResponse?.Output.FirstOrDefault(item => item.Content.Any(content => !string.IsNullOrWhiteSpace(content.Text)));
            var text = output?.Content.Select(content => content.Text).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            
            if (string.IsNullOrWhiteSpace(text)) throw new ModelProviderException("The model provider response contained no content.", response.StatusCode);
            return new GeneratedResponse(Text: text, Role: output?.Role ?? "assistant", ProviderResponseId: qwenResponse?.Id);
        }
    }
}

