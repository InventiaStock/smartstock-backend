using System.Security.Cryptography;
using System.Text;

namespace SmartStock.Platform.API.IAM;

// Contracts of the IAM context (TS04). The frontend already uses these names:
// do not rename them. (Contratos de IAM: el frontend ya usa estos nombres.)

public record RegisterCommand(string? BusinessName, string? Email, string? Password, string? BusinessType);

public record LoginCommand(string? Email, string? Password);

public record ForgotPasswordCommand(string? Email);

public record ResetPasswordCommand(string? Token, string? Password);

public record SessionResource(string Token, string Email, string BusinessName, string BusinessType);

public record ForgotPasswordResource(bool Sent, string Email, int ExpiresInHours);

public interface ITokenService
{
    string CreateToken(User user, Business business);
}

public interface IRegistrationService
{
    Task<SessionResource> HandleAsync(RegisterCommand command);
}

public interface ILoginService
{
    Task<SessionResource> HandleAsync(LoginCommand command);
}

public interface IPasswordRecoveryService
{
    Task<ForgotPasswordResource> HandleAsync(ForgotPasswordCommand command);

    Task HandleAsync(ResetPasswordCommand command);
}

/// <summary>Claim names inside the token. (Nombres de los datos dentro del token.)</summary>
public static class AuthClaims
{
    public const string BusinessId = "business_id";
    public const string Email = "email";
}

public static class EmailRules
{
    public static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValid(string email)
    {
        if (email.Length is 0 or > 160 || email.Contains(' ')) return false;
        var at = email.IndexOf('@');
        return at > 0 && at == email.LastIndexOf('@')
            && email.LastIndexOf('.') > at + 1 && !email.EndsWith('.');
    }
}

public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>BCrypt only reads the first 72 bytes. (BCrypt solo lee los primeros 72 bytes.)</summary>
    public const int MaxBytes = 72;

    public static bool IsValid(string? password) =>
        !string.IsNullOrWhiteSpace(password)
        && password.Length >= MinLength
        && Encoding.UTF8.GetByteCount(password) <= MaxBytes;
}

public static class ResetTokens
{
    /// <summary>R4: the reset link lasts 24 hours.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public static string Create() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    /// <summary>Only the hash is saved in the database. (En la base solo se guarda el hash.)</summary>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
