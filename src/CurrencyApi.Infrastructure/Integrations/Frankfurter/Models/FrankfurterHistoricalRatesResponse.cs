using System.Text.Json.Serialization;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Models;

public sealed class FrankfurterHistoricalRatesResponse
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("base")]
    public string? Base { get; init; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; init; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; init; }

    [JsonPropertyName("rates")]
    public Dictionary<string, Dictionary<string, decimal>>? Rates { get; init; }
}
