namespace SmartStock.Platform.API.Shared.Contracts;

// Resources used by more than one context. The JSON names in Spanish come from WireNamingPolicy.
// (Recursos usados por varios contextos. Los nombres JSON en español salen de WireNamingPolicy.)

/// <summary>Product as the frontend shows it (with the sensor information). Used by Catalog, Stock and Analytics.</summary>
public record ProductViewResource(
    long Id,
    string Name,
    string Sku,
    string Category,
    decimal UnitWeight,
    decimal? SalePrice,
    decimal? PurchaseCost,
    string UsualSupplier,
    int RegisteredStock,
    int MinThreshold,
    int MaxCapacity,
    long? SensorId,
    string SensorStatus,
    int? SensorUnits,
    int ReferenceStock,
    string StockLevel);

public static class ProductViewFactory
{
    public static ProductViewResource Create(ProductSnapshot product, SensorSnapshot? sensor)
    {
        var status = sensor?.Status ?? SensorStatuses.None;

        // Only an online sensor gives the physical units (R11, R12).
        int? sensorUnits = sensor is { Status: SensorStatuses.Online }
            ? InventoryRules.ToUnits(sensor.WeightKg, product.UnitWeightKg)
            : null;

        var referenceStock = sensorUnits ?? product.RegisteredStock;

        return new ProductViewResource(
            product.Id, product.Name, product.Sku, product.Category, product.UnitWeightKg,
            product.SalePrice, product.PurchaseCost, product.UsualSupplier,
            product.RegisteredStock, product.MinThreshold, product.MaxCapacity,
            sensor?.Id, status, sensorUnits, referenceStock,
            InventoryRules.StockLevel(referenceStock, product.MinThreshold));
    }
}

public record StockMovementResource(
    long Id,
    long ProductId,
    string Type,
    string Origin,
    string OriginCode,
    int Quantity,
    DateOnly Date,
    string ProductName)
{
    public static StockMovementResource From(StockMovementSnapshot m) =>
        new(m.Id, m.ProductId, m.Type, m.Origin, m.OriginCode, m.Quantity, m.Date, m.ProductName);
}

/// <summary>Answer of the POST endpoints that create something with a text code (R-00042, PO-00032).</summary>
public record CreatedCodeResource(string Id);

/// <summary>Answer of the POST endpoints that create something with a numeric id.</summary>
public record CreatedIdResource(long Id);
