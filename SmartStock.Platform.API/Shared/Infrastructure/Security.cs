namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>
/// The current user of the request. By indication of the professor the endpoints are open for now
/// (no login, no token), so every request works as ONE fixed business (id 1 by default).
/// (Por ahora los endpoints van abiertos: toda solicitud trabaja como un negocio fijo, el id 1.)
/// </summary>
public interface ICurrentUser
{
    long BusinessId { get; }

    /// <summary>Email of the default notification channel (Notifications:DefaultEmail).</summary>
    string? Email { get; }

    /// <summary>
    /// For the IoT devices: after finding the sensor, work as its business.
    /// (Para los sensores: trabajar como el negocio del sensor.)
    /// </summary>
    void UseBusiness(long businessId);
}

public class CurrentUser(IConfiguration configuration) : ICurrentUser
{
    private long? _businessOverride;

    public long BusinessId => _businessOverride ?? configuration.GetValue("Tenant:DefaultBusinessId", 1L);

    public string? Email => configuration["Notifications:DefaultEmail"];

    public void UseBusiness(long businessId) => _businessOverride = businessId;
}
