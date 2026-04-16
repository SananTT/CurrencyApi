namespace CurrencyApi.Api.Authentication;

public sealed class JwtAuthSettings
{
    public string Issuer { get; init; } = "CurrencyApi";

    public string Audience { get; init; } = "CurrencyApiClients";

    public string SigningKey { get; init; } = "development-signing-key-change-me-1234567890";

    public int ExpiryMinutes { get; init; } = 60;
}
