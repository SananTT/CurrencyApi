namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Configuration;

public sealed class FrankfurterResilienceSettings
{
    public int TimeoutMilliseconds { get; init; } = 3000;

    public int MaxRetryAttempts { get; init; } = 2;

    public int BaseDelayMilliseconds { get; init; } = 200;

    public int CircuitBreakerFailureThreshold { get; init; } = 3;

    public int CircuitBreakerBreakSeconds { get; init; } = 30;
}
