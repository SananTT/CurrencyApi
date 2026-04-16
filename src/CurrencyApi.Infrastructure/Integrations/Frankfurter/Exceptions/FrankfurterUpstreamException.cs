using System.Net;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Exceptions;

public sealed class FrankfurterUpstreamException : Exception
{
    public FrankfurterUpstreamException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
