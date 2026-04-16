namespace CurrencyApi.Api.Observability;

public sealed class OutboundCorrelationHandler : DelegatingHandler
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public OutboundCorrelationHandler(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationHeaders.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(
                CorrelationHeaders.CorrelationId,
                _correlationContextAccessor.GetCorrelationId());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
