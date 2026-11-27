namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>
/// The current user of the request. With a valid token, the request works as the business of the user.
/// Without a token it works as the default (demo) business, because the other endpoints are still open.
/// (Con token: negocio del usuario. Sin token: negocio demo, porque los demás endpoints siguen abiertos.)
/// </summary>
public interface ICurrentUser
{
    long BusinessId { get; }

    /// <summary>Email of the signed-in user, or Notifications:DefaultEmail.</summary>
    string? Email { get; }

    /// <summary>
    /// For the IoT devices: after finding the sensor, work as its business.
    /// (Para los sensores: trabajar como el negocio del sensor.)
    /// </summary>
    void UseBusiness(long businessId);
}

public class CurrentUser(IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private long? _businessOverride;

    public long BusinessId =>
        _businessOverride ?? TokenBusinessId() ?? configuration.GetValue("Tenant:DefaultBusinessId", 1L);

    public string? Email => Claim("email") ?? configuration["Notifications:DefaultEmail"];

    public void UseBusiness(long businessId) => _businessOverride = businessId;

    private long? TokenBusinessId() => long.TryParse(Claim("business_id"), out var id) ? id : null;

    private string? Claim(string type) => httpContextAccessor.HttpContext?.User.FindFirst(type)?.Value;
}