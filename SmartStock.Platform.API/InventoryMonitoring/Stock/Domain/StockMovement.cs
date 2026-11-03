namespace SmartStock.Platform.API.InventoryMonitoring.Stock;

/// <summary>
/// One entry or exit of stock. R17: the registered stock only changes through movements.
/// Type: IN / OUT. Origin: SALE / PURCHASE / ADJUSTMENT. OriginCode: R-00042, PO-00032 or INITIAL.
/// </summary>
public class StockMovement : IHasId, ITenantEntity
{
    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public long ProductId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Origin { get; private set; } = string.Empty;
    public string OriginCode { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StockMovement() { }   // for EF Core (para EF Core)

    public StockMovement(long productId, string type, string origin, string originCode, int quantity, DateOnly date)
    {
        ProductId = productId;
        Type = type;
        Origin = origin;
        OriginCode = originCode;
        Quantity = quantity;
        Date = date;
        CreatedAt = Clock.UtcNow;
    }
}

public interface IStockMovementRepository : IBaseRepository<StockMovement>
{
    Task<IReadOnlyList<StockMovement>> ListByDateRangeAsync(DateOnly? from, DateOnly? to);

    Task<IReadOnlyList<StockMovement>> ListByOriginAsync(string origin, string originCode);
}
