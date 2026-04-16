namespace CurrencyApi.Application.Auth.Contracts;

public static class SystemRoles
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static string Normalize(string role)
    {
        if (string.Equals(role, User, StringComparison.OrdinalIgnoreCase))
        {
            return User;
        }

        if (string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Admin;
        }

        throw new InvalidOperationException("Unsupported user role configured.");
    }
}
