using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;

namespace CurrencyApi.Application.Admin.UseCases.ClearCache;

public sealed class ClearCacheUseCase : IClearCacheUseCase
{
    private readonly IRatesCacheInvalidator _cacheInvalidator;
    private readonly IValidator<ClearCacheRequest> _validator;

    public ClearCacheUseCase(
        IRatesCacheInvalidator cacheInvalidator,
        IValidator<ClearCacheRequest> validator)
    {
        _cacheInvalidator = cacheInvalidator;
        _validator = validator;
    }

    public async Task<ClearCacheResponse> ExecuteAsync(
        ClearCacheRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var invalidatedEntries = await _cacheInvalidator.InvalidateAsync(
            request.ProviderKind,
            cancellationToken);

        return new ClearCacheResponse(
            ProviderKind: request.ProviderKind,
            InvalidatedEntries: invalidatedEntries);
    }
}
