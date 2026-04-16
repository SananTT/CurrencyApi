using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Admin.UseCases.ClearCache;
using CurrencyApi.Application.Admin.Validation;
using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Providers.Contracts;

namespace CurrencyApi.UnitTests.Application.Admin.UseCases;

public sealed class ClearCacheUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnInvalidatedEntryCount_WhenRequestIsValid()
    {
        var useCase = new ClearCacheUseCase(
            cacheInvalidator: new StubRatesCacheInvalidator(4),
            validator: new ClearCacheRequestValidator());

        var result = await useCase.ExecuteAsync(new ClearCacheRequest(CurrencyProviderKind.Frankfurter));

        Assert.Equal(CurrencyProviderKind.Frankfurter, result.ProviderKind);
        Assert.Equal(4, result.InvalidatedEntries);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowRequestValidationException_WhenProviderKindIsInvalid()
    {
        var useCase = new ClearCacheUseCase(
            cacheInvalidator: new StubRatesCacheInvalidator(0),
            validator: new ClearCacheRequestValidator());

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            useCase.ExecuteAsync(new ClearCacheRequest((CurrencyProviderKind)999)));
    }

    private sealed class StubRatesCacheInvalidator : IRatesCacheInvalidator
    {
        private readonly int _invalidatedEntries;

        public StubRatesCacheInvalidator(int invalidatedEntries)
        {
            _invalidatedEntries = invalidatedEntries;
        }

        public Task<int> InvalidateAsync(
            CurrencyProviderKind? providerKind,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_invalidatedEntries);
        }
    }
}
