using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SmartStock.Platform.API.IAM;

/// <summary>Section "Jwt" of the configuration. The secret NEVER goes into the repository.</summary>
public class JwtSettings
{
    public const string Section = "Jwt";

    /// <summary>HS256 needs a key of at least 32 bytes. (Mínimo 32 caracteres.)</summary>
    public const int MinSecretBytes = 32;

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "smartstock-api";
    public string Audience { get; set; } = "smartstock-web";
    public int ExpirationHours { get; set; } = 8;

    public SymmetricSecurityKey SigningKey()
    {
        var bytes = Encoding.UTF8.GetBytes(Secret);
        if (bytes.Length < MinSecretBytes)
        {
            throw new InvalidOperationException(
                "Jwt:Secret is missing or too short. Set the Jwt__Secret environment variable (32+ characters).");
        }

        return new SymmetricSecurityKey(bytes);
    }
}
