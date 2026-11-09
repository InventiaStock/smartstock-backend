namespace SmartStock.Platform.API.AlertsRestocking;

/// <summary>
/// An alert of the business. Type LOW_STOCK (R19) or DISCREPANCY (R20, over 10 %).
/// Source: "sensor" when the sensor was online, "registered" otherwise.
/// </summary>
public class Alert : IHasId, ITenantEntity
{
    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public string Type { get; private set; } = string.Empty;
    public long ProductId { get; private set; }
    public int ReferenceStock { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Status { get; private set; } = AlertStatuses.Active;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    // Only for DISCREPANCY alerts (solo para alertas de discrepancia)
    public int? RegisteredStock { get; private set; }
    public int? PhysicalStock { get; private set; }
    public double? DifferencePct { get; private set; }

    private Alert() { }   // for EF Core (para EF Core)

    public static Alert CreateLowStock(long productId, int referenceStock, string source) => new()
    {
        Type = AlertTypes.LowStock,
        ProductId = productId,
        ReferenceStock = referenceStock,
        Source = source,
        CreatedAt = Clock.UtcNow,
    };

    public static Alert CreateDiscrepancy(long productId, int registeredStock, int physicalStock, double differencePct) => new()
    {
        Type = AlertTypes.Discrepancy,
        ProductId = productId,
        ReferenceStock = registeredStock,
        Source = "sensor",
        RegisteredStock = registeredStock,
        PhysicalStock = physicalStock,
        DifferencePct = Math.Round(differencePct, 1),
        CreatedAt = Clock.UtcNow,
    };

    public void Resolve()
    {
        Status = AlertStatuses.Resolved;
        ResolvedAt = Clock.UtcNow;
    }

    public bool IsActive => Status == AlertStatuses.Active;

    /// <summary>Only for the demo seeder.</summary>
    public void SetCreatedAtForSeed(DateTime createdAt) => CreatedAt = createdAt;
}

/// <summary>R19/R21: created together with a LOW_STOCK alert. PENDING, then IN_PROGRESS (purchase), then COMPLETED.</summary>
public class RestockingNeed : IHasId, ITenantEntity
{
    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public long ProductId { get; private set; }
    public long AlertId { get; private set; }
    public Alert? Alert { get; private set; }
    public string Status { get; private set; } = NeedStatuses.Pending;
    public string? PurchaseCode { get; private set; }

    private RestockingNeed() { }

    public RestockingNeed(Alert alert)
    {
        Alert = alert;
        ProductId = alert.ProductId;
    }

    public void StartPurchase(string purchaseCode)
    {
        Status = NeedStatuses.InProgress;
        PurchaseCode = purchaseCode;
    }

    public void Complete() => Status = NeedStatuses.Completed;
}

/// <summary>Notification channel of the business: email or whatsapp (at least one must stay active).</summary>
public class NotificationChannel : IHasId, ITenantEntity
{
    public const string Email = "email";
    public const string WhatsApp = "whatsapp";

    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public string Channel { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public bool Active { get; private set; }

    private NotificationChannel() { }

    public NotificationChannel(string channel, string destination, bool active)
    {
        Channel = channel;
        Destination = destination;
        Active = active;
    }

    public void SetActive(bool active) => Active = active;
}

/// <summary>Record of each notification sent (or failed) through an external service.</summary>
public class SentNotification : IHasId, ITenantEntity
{
    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public long AlertId { get; private set; }
    public Alert? Alert { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }
    public string? Error { get; private set; }

    private SentNotification() { }

    public SentNotification(Alert alert, string channel, string destination, string? error)
    {
        Alert = alert;
        Channel = channel;
        Destination = destination;
        SentAt = Clock.UtcNow;
        Error = error;
    }
}

public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<Alert?> FindActiveAsync(long productId, string type);

    Task<Alert?> FindActiveByProductAsync(long productId);

    /// <summary>ACTIVE alerts plus the ones resolved in the last 24 hours (newest first).</summary>
    Task<IReadOnlyList<Alert>> ListVisibleAsync();

    Task<int> CountActiveAsync(string type);
}

public interface IRestockingNeedRepository : IBaseRepository<RestockingNeed>
{
    Task<IReadOnlyList<RestockingNeed>> ListByAlertIdsAsync(IEnumerable<long> alertIds);
}

public interface INotificationChannelRepository : IBaseRepository<NotificationChannel> { }

public interface ISentNotificationRepository : IBaseRepository<SentNotification> { }
