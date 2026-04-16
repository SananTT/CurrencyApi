using CurrencyApi.Application.Providers.Contracts;

namespace CurrencyApi.Infrastructure.Providers;

public sealed class CurrencyProviderFactory : ICurrencyProviderFactory
{
    private readonly IReadOnlyDictionary<CurrencyProviderKind, ICurrencyRatesProvider> _providers;

    public CurrencyProviderFactory(IEnumerable<ICurrencyRatesProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var providerMap = new Dictionary<CurrencyProviderKind, ICurrencyRatesProvider>();

        foreach (var provider in providers)
        {
            if (!providerMap.TryAdd(provider.Kind, provider))
            {
                throw new InvalidOperationException(
                    $"Multiple currency providers were registered for kind '{provider.Kind}'.");
            }
        }

        _providers = providerMap;
    }

    public ICurrencyRatesProvider Create(CurrencyProviderKind providerKind)
    {
        if (_providers.TryGetValue(providerKind, out var provider))
        {
            return provider;
        }

        throw new KeyNotFoundException(
            $"No currency provider is registered for kind '{providerKind}'.");
    }
}
