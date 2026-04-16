namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Configuration;

public sealed class FrankfurterClientSettings
{
    public const string DefaultBaseUrl = "https://api.frankfurter.app/";

    public string BaseUrl { get; init; } = DefaultBaseUrl;
}
