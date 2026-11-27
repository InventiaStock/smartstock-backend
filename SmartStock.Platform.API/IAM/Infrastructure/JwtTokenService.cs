using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SmartStock.Platform.API.IAM;

/// <summary>TS04: creates the signed token with the user and its business. (Crea el token firmado.)</summary>
public class JwtTokenService(IOptions<JwtSettings> options) : ITokenService
{
    public string CreateToken(User user, Business business)
    {
        var settings = options.Value;
        var now = Clock.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddHours(settings.ExpirationHours),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", user.Id.ToString()),
                new Claim(AuthClaims.Email, user.Email),
                new Claim(AuthClaims.BusinessId, business.Id.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(settings.SigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
