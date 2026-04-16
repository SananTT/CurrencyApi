namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;

public interface IFrankfurterResiliencePipeline
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
