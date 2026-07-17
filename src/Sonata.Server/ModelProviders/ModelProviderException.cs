using System.Net;

namespace Sonata.Server.ModelProviders;

public sealed class ModelProviderException : Exception
{
    public ModelProviderException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null) :
        base(message, innerException)
    {
        StatusCode = statusCode;
    }
    
    public HttpStatusCode? StatusCode { get; }
}

