using CurrencyApi.Application.Admin.Contracts;

namespace CurrencyApi.Application.Admin.UseCases.ClearCache;

public interface IClearCacheUseCase
{
    Task<ClearCacheResponse> ExecuteAsync(
        ClearCacheRequest request,
        CancellationToken cancellationToken = default);
}
