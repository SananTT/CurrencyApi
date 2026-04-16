namespace CurrencyApi.Infrastructure.Caching.Configuration;

public sealed class RatesCacheSettings
{
    public int LatestTtlSeconds { get; init; } = 30;

    public int HistoricalTtlSeconds { get; init; } = 300;
}
