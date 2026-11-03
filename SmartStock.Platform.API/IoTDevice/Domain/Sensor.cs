namespace SmartStock.Platform.API.IoTDevice;

/// <summary>Weight sensor. It verifies the stock, it never changes the registered stock (R18).</summary>
public class Sensor : IHasId, ITenantEntity
{
    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public string Code { get; private set; } = string.Empty;
    public long? ProductId { get; private set; }
    public decimal WeightKg { get; private set; }
    public decimal? ReferenceWeightKg { get; private set; }
    public DateTime? LastReadingAt { get; private set; }

    private Sensor() { }   // for EF Core (para EF Core)

    public Sensor(string code, decimal weightKg)
    {
        Code = code;
        WeightKg = weightKg;
    }

    /// <summary>R9: links the sensor to a product. The current weight becomes the reference weight.</summary>
    public void LinkTo(long productId)
    {
        ProductId = productId;
        ReferenceWeightKg = WeightKg;
        LastReadingAt = Clock.UtcNow;
    }

    public void RegisterReading(decimal weightKg, DateTime readAt)
    {
        WeightKg = weightKg;
        LastReadingAt = readAt;
    }

    /// <summary>Only for the demo seeder.</summary>
    public void SetStateForSeed(long? productId, DateTime? lastReadingAt)
    {
        ProductId = productId;
        ReferenceWeightKg = productId is null ? null : WeightKg;
        LastReadingAt = lastReadingAt;
    }

    public string Status(DateTime nowUtc) => InventoryRules.SensorStatus(ProductId, LastReadingAt, nowUtc);
}

public class SensorReading : IHasId
{
    public long Id { get; private set; }
    public long SensorId { get; private set; }
    public decimal WeightKg { get; private set; }
    public DateTime ReadAt { get; private set; }

    private SensorReading() { }

    public SensorReading(long sensorId, decimal weightKg, DateTime readAt)
    {
        SensorId = sensorId;
        WeightKg = weightKg;
        ReadAt = readAt;
    }
}

public interface ISensorRepository : IBaseRepository<Sensor>
{
    /// <summary>Used by the devices, which send no token: it skips the business filter.</summary>
    Task<Sensor?> FindByIdIgnoringTenantAsync(long sensorId);

    Task<Sensor?> FindByProductIdAsync(long productId);

    Task AddReadingAsync(SensorReading reading);

    Task<IReadOnlyList<SensorReading>> GetLatestReadingsAsync(long sensorId, int take);
}
