namespace SmartStock.Platform.API.IAM;

/// <summary>US02, TS04, R3: checks the credentials and returns the session. (Inicio de sesión.)</summary>
public class LoginService(
    IUserRepository users,
    IBusinessRepository businesses,
    ITokenService tokens) : ILoginService
{
    public async Task<SessionResource> HandleAsync(LoginCommand command)
    {
        var email = EmailRules.Normalize(command.Email);
        if (email.Length == 0 || string.IsNullOrEmpty(command.Password))
            throw new DomainException("INVALID_CREDENTIALS", 401);

        var user = await users.FindByEmailAsync(email);

        // R3: the same error for an unknown email and for a wrong password.
        // (El mismo error si falla el correo o la contraseña.)
        if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
            throw new DomainException("INVALID_CREDENTIALS", 401);

        var business = await businesses.FindByIdAsync(user.BusinessId)
            ?? throw new DomainException("INVALID_CREDENTIALS", 401);

        return new SessionResource(
            tokens.CreateToken(user, business), user.Email, business.Name, business.BusinessType);
    }
}