using System.Net;

namespace SmartStock.Platform.API.IAM;

public static class PasswordResetEmail
{
    public const string Subject = "SmartStock - Reset your password / Restablece tu contraseña";

    public static string Link(string frontendBaseUrl, string token) =>
        $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

    public static string Body(string link)
    {
        var safeLink = WebUtility.HtmlEncode(link);

        return $"""
                <p>Open this link to set a new password. It expires in 24 hours.</p>
                <p>Abre este enlace para crear una nueva contraseña. Vence en 24 horas.</p>
                <p><a href="{safeLink}">{safeLink}</a></p>
                """;
    }
}