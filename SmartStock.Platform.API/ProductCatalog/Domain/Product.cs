namespace SmartStock.Platform.API.ProductCatalog;

/// <summary>
/// Product of the catalog. TS13: sale_price, purchase_cost and the usual supplier live here.
/// R17: RegisteredStock has a private setter, it only changes through ChangeRegisteredStock,
/// which the stock movement engine calls. (El stock registrado solo cambia por movimientos.)
/// </summary>
public class Product : IHasId, ITenantEntity
{
    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal UnitWeightKg { get; private set; }
    public decimal? SalePrice { get; private set; }
    public decimal? PurchaseCost { get; private set; }
    public string UsualSupplier { get; private set; } = string.Empty;
    public int RegisteredStock { get; private set; }
    public int MinThreshold { get; private set; }
    public int MaxCapacity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Product() { }   // for EF Core (para EF Core)

    public Product(
        string name, string sku, string category, decimal unitWeightKg, decimal? salePrice,
        decimal? purchaseCost, string usualSupplier, int minThreshold, int maxCapacity)
    {
        UpdateDetails(name, sku, category, unitWeightKg, salePrice, purchaseCost, usualSupplier, minThreshold, maxCapacity);
        CreatedAt = Clock.UtcNow;
    }

    public void UpdateDetails(
        string name, string sku, string category, decimal unitWeightKg, decimal? salePrice,
        decimal? purchaseCost, string usualSupplier, int minThreshold, int maxCapacity)
    {
        Name = name;
        Sku = sku;
        Category = category;
        UnitWeightKg = unitWeightKg;
        SalePrice = salePrice;
        PurchaseCost = purchaseCost;
        UsualSupplier = usualSupplier;
        MinThreshold = minThreshold;
        MaxCapacity = maxCapacity;
    }

    public void ChangeMinThreshold(int minThreshold) => MinThreshold = minThreshold;

    /// <summary>Adds (positive) or subtracts (negative) units. Never leaves the stock below zero.</summary>
    public int ChangeRegisteredStock(int delta)
    {
        var next = RegisteredStock + delta;
        if (next < 0)
        {
            throw DomainException.Unprocessable("INSUFFICIENT_STOCK", DomainException.With(
                ("productoId", Id), ("productoNombre", Name), ("available", RegisteredStock)));
        }

        RegisteredStock = next;
        return next;
    }

    /// <summary>Only for the demo seeder. (Solo para el seeder de datos demo.)</summary>
    public void SetStockForSeed(int stock) => RegisteredStock = stock;
}

public interface IProductRepository : IBaseRepository<Product> { }
