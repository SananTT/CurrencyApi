namespace CurrencyApi.Api.RateLimiting;

public sealed class RateLimitingSettings
{
    public EndpointRateLimitSettings Login { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60,
        QueueLimit = 0,
    };

    public EndpointRateLimitSettings Historical { get; init; } = new()
    {
        PermitLimit = 3,
        WindowSeconds = 60,
        QueueLimit = 0,
    };
}
