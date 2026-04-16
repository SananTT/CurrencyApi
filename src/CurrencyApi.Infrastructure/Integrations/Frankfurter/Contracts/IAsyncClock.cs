namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;

public interface IAsyncClock
{
    DateTimeOffset UtcNow { get; }

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
