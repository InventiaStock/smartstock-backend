namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>Names of the claims inside the JWT. (Nombres de los claims del JWT.)</summary>
public static class AuthClaims
{
    public const string UserId = "uid";
    public const string BusinessId = "bid";
    public const string Email = "email";
    public const string BusinessType = "btype";
}

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;   // at least 32 characters (mínimo 32 caracteres)
    public string Issuer { get; set; } = "SmartStock";
    public string Audience { get; set; } = "SmartStockWeb";
    public int ExpiresMinutes { get; set; } = 480;
}

/// <summary>The logged user of the current request. (El usuario autenticado de la solicitud.)</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    long UserId { get; }
    long BusinessId { get; }
    string? Email { get; }

    /// <summary>
    /// For requests without token (the IoT devices): after finding the sensor, work as its business.
    /// (Para solicitudes sin token, como los sensores: trabajar como el negocio del sensor.)
    /// </summary>
    void UseBusiness(long businessId);
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private long? _businessOverride;

    private System.Security.Claims.ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long UserId => ReadLong(AuthClaims.UserId);

    public long BusinessId => _businessOverride ?? ReadLong(AuthClaims.BusinessId);

    public string? Email => Principal?.FindFirst(AuthClaims.Email)?.Value;

    public void UseBusiness(long businessId) => _businessOverride = businessId;

    private long ReadLong(string claimType) =>
        long.TryParse(Principal?.FindFirst(claimType)?.Value, out var value) ? value : 0;
}
