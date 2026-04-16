namespace CurrencyApi.Api.RateLimiting;

public sealed class EndpointRateLimitSettings
{
    public int PermitLimit { get; init; } = 5;

    public int WindowSeconds { get; init; } = 60;

    public int QueueLimit { get; init; } = 0;
}
