namespace CurrencyApi.Application.Providers.Contracts;

public interface ICurrencyProviderFactory
{
    ICurrencyRatesProvider Create(CurrencyProviderKind providerKind);
}
