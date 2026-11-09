namespace SmartStock.Platform.API.InventoryMonitoring.Sales;

/// <summary>A registered sale (US26). Code R-00042 = "R-" + business sequence number. (Venta registrada.)</summary>
public class Sale : IHasId, ITenantEntity
{
    public const string CodePrefix = "R";

    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public int Number { get; private set; }
    public DateOnly Date { get; private set; }
    public string Status { get; private set; } = SaleStatuses.Completed;
    public decimal Total { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<SaleItem> Items { get; private set; } = [];

    public string Code => CodeFormat.Format(CodePrefix, Number);

    private Sale() { }   // for EF Core (para EF Core)

    public Sale(int number, DateOnly date, IEnumerable<SaleItem> items)
    {
        Number = number;
        Date = date;
        Items = items.ToList();
        Total = InventoryRules.Money(Items.Sum(item => item.Quantity * item.UnitPrice));
        CreatedAt = Clock.UtcNow;
    }
}

public class SaleItem : IHasId
{
    public long Id { get; private set; }
    public long SaleId { get; private set; }
    public long ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private SaleItem() { }

    public SaleItem(long productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

public interface ISaleRepository : IBaseRepository<Sale>
{
    Task<int> NextNumberAsync();

    Task<Sale?> FindByCodeAsync(string code);

    Task<IReadOnlyList<Sale>> ListByDateRangeAsync(DateOnly? from, DateOnly? to);
}
