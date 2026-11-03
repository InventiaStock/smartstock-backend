namespace SmartStock.Platform.API.InventoryMonitoring.Procurement;

public class Supplier : IHasId, ITenantEntity
{
    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Supplier() { }   // for EF Core (para EF Core)

    public Supplier(string name, string phone, string? email)
    {
        Name = name.Trim();
        Phone = phone.Trim();
        Email = email?.Trim() ?? string.Empty;
        CreatedAt = Clock.UtcNow;
    }

    /// <summary>R8: the name + phone combination is not repeated (ignoring case and spaces).</summary>
    public bool IsSameAs(string name, string phone) =>
        string.Equals(Name, name.Trim(), StringComparison.OrdinalIgnoreCase) &&
        Normalize(Phone) == Normalize(phone);

    private static string Normalize(string phone) => phone.Replace(" ", string.Empty);
}

/// <summary>
/// A purchase to a supplier (US30). Starts PENDING and does NOT change the stock (R15);
/// receiving it creates the IN movements (R16). Code PO-00032.
/// </summary>
public class Purchase : IHasId, ITenantEntity
{
    public const string CodePrefix = "PO";

    public long Id { get; private set; }
    public long BusinessId { get; set; }
    public int Number { get; private set; }
    public long SupplierId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Status { get; private set; } = PurchaseStatuses.Pending;
    public decimal Total { get; private set; }
    public long? RestockingNeedId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<PurchaseItem> Items { get; private set; } = [];

    public string Code => CodeFormat.Format(CodePrefix, Number);

    private Purchase() { }

    public Purchase(int number, long supplierId, DateOnly date, long? restockingNeedId, IEnumerable<PurchaseItem> items)
    {
        Number = number;
        SupplierId = supplierId;
        Date = date;
        RestockingNeedId = restockingNeedId;
        Items = items.ToList();
        Total = InventoryRules.Money(Items.Sum(item => item.Quantity * item.UnitCost));
        CreatedAt = Clock.UtcNow;
    }

    public void MarkReceived()
    {
        if (Status == PurchaseStatuses.Received) throw DomainException.Conflict("ALREADY_RECEIVED");
        Status = PurchaseStatuses.Received;
    }

    /// <summary>Only for the demo seeder.</summary>
    public void SetStatusForSeed(string status) => Status = status;
}

public class PurchaseItem : IHasId
{
    public long Id { get; private set; }
    public long PurchaseId { get; private set; }
    public long ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }

    private PurchaseItem() { }

    public PurchaseItem(long productId, int quantity, decimal unitCost)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitCost = unitCost;
    }
}

public interface ISupplierRepository : IBaseRepository<Supplier> { }

public interface IPurchaseRepository : IBaseRepository<Purchase>
{
    Task<int> NextNumberAsync();

    Task<Purchase?> FindByCodeAsync(string code);

    Task<IReadOnlyList<Purchase>> ListByDateRangeAsync(DateOnly? from, DateOnly? to);
}
