using SmartStock.Platform.API.Shared.Domain;

namespace SmartStock.Platform.API.IAM;

/// <summary>Validated data of a new account.</summary>
public record RegistrationData(
    string BusinessName,
    string Email,
    string Password,
    string BusinessType);

/// <summary>US01: all registration data must be valid.</summary>
public static class RegistrationRules
{
    private static readonly string[] BusinessTypes =
        ["minimarket", "bodega"];

    public static RegistrationData Validate(RegisterCommand command)
    {
        var name = (command.BusinessName ?? string.Empty).Trim();
        var email = EmailRules.Normalize(command.Email);
        var type = (command.BusinessType ?? string.Empty)
            .Trim()
            .ToLowerInvariant();

        if (name.Length is 0 or > 120
            || !EmailRules.IsValid(email)
            || !PasswordPolicy.IsValid(command.Password)
            || !BusinessTypes.Contains(type))
        {
            throw DomainException.BadRequest("INCOMPLETE");
        }

        return new RegistrationData(
            name,
            email,
            command.Password!,
            type);
    }
}
