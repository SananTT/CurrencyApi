using CurrencyApi.Application.Auth.Contracts;

namespace CurrencyApi.Application.Auth.UseCases.Login;

public interface ILoginUseCase
{
    Task<LoginResponse> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
