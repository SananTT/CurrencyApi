using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Resilience;

public sealed class SystemAsyncClock : IAsyncClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
        Task.Delay(delay, cancellationToken);
}
