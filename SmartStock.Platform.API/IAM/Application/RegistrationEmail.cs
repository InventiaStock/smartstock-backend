using System.Net;

namespace SmartStock.Platform.API.IAM;

/// <summary>US01: confirmation email of the registration. (Correo de confirmación del registro.)</summary>
public static class RegistrationEmail
{
    public const string Subject = "SmartStock - Account created / Cuenta creada";

    public static string Body(string businessName)
    {
        var name = WebUtility.HtmlEncode(businessName);
        return $"""
            <p>Your SmartStock account for <strong>{name}</strong> is ready.</p>
            <p>Tu cuenta de SmartStock para <strong>{name}</strong> ya está lista.</p>
            """;
    }
}
