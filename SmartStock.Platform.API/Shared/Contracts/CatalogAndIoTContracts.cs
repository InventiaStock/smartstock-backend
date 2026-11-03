namespace SmartStock.Platform.API.Shared.Contracts;

// ───────────────────────── Product Catalog (owner: Candy) ─────────────────────────

public record ProductSnapshot(
    long Id,
    string Name,
    string Sku,
    string Category,
    decimal UnitWeightKg,
    decimal? SalePrice,
    decimal? PurchaseCost,
    string UsualSupplier,
    int RegisteredStock,
    int MinThreshold,
    int MaxCapacity);

/// <summary>
/// What other contexts may ask the Product Catalog. Implemented by Candy.
/// (Lo que otros contextos pueden pedirle al catálogo. Lo implementa Candy.)
/// </summary>
public interface IProductCatalogFacade
{
    Task<ProductSnapshot?> FindByIdAsync(long productId);

    Task<IReadOnlyList<ProductSnapshot>> ListAsync();

    /// <summary>
    /// R17: the registered stock only changes through stock movements, so ONLY the stock movement
    /// engine may call this. Returns the new stock. Does not save (the caller's unit of work saves).
    /// (El stock solo cambia por movimientos: solo el motor de movimientos debe llamarlo.)
    /// </summary>
    Task<int> ChangeRegisteredStockAsync(long productId, int delta);
}

// ───────────────────────── IoT Device (owner: Ruth) ─────────────────────────

public record SensorSnapshot(
    long Id,
    string Code,
    long? ProductId,
    decimal WeightKg,
    DateTime? LastReadingAt,
    string Status);

public record ReadingSnapshot(long Id, long SensorId, decimal WeightKg, DateTime ReadAt);

/// <summary>What other contexts may ask the IoT Device context. Implemented by Ruth.</summary>
public interface IIoTDeviceFacade
{
    Task<SensorSnapshot?> FindByIdAsync(long sensorId);

    Task<SensorSnapshot?> FindByProductIdAsync(long productId);

    Task<IReadOnlyList<SensorSnapshot>> ListAsync();

    Task<IReadOnlyList<ReadingSnapshot>> GetLatestReadingsAsync(long sensorId, int take);
}
