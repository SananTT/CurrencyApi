using System.Text.Json.Serialization;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Models;

public sealed class FrankfurterLatestRatesResponse
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("base")]
    public string? Base { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal>? Rates { get; init; }
}
