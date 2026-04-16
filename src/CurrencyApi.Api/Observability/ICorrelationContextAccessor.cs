namespace CurrencyApi.Api.Observability;

public interface ICorrelationContextAccessor
{
    string GetCorrelationId();
}
