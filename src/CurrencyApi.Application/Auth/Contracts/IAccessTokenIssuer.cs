namespace CurrencyApi.Application.Auth.Contracts;

public interface IAccessTokenIssuer
{
    AccessTokenResult IssueToken(AuthenticatedUser user);
}
