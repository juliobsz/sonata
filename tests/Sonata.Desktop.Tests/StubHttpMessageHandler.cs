namespace Sonata.Desktop.Tests;

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    public HttpMethod? ReceivedMethod { get; private set; }
    public Uri? ReceivedUri { get; private set; }
    public string? ReceivedContent { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ReceivedMethod = request.Method;
        ReceivedUri = request.RequestUri;
        ReceivedContent = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return respond(request);
    }
}