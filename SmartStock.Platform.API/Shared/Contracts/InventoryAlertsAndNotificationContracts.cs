namespace SmartStock.Platform.API.Shared.Contracts;

// ───────────────── Inventory Monitoring: stock movements (owner: Geraldine) ─────────────────

public record RegisterMovementRequest(long ProductId, string Type, string Origin, string OriginCode, int Quantity);

public record StockMovementSnapshot(
    long Id,
    long ProductId,
    string ProductName,
    string Type,
    string Origin,
    string OriginCode,
    int Quantity,
    DateOnly Date);

public interface IStockMovementFacade
{
    /// <summary>
    /// TS14 engine: changes the registered stock (through the catalog), stores the movement and asks
    /// Alerts to evaluate the rules. It does NOT save: the caller's IUnitOfWork saves everything at once.
    /// (Motor de movimientos: no guarda; quien llama guarda todo junto con IUnitOfWork.)
    /// </summary>
    Task RegisterMovementAsync(RegisterMovementRequest request);

    Task<IReadOnlyList<StockMovementSnapshot>> ListAsync(DateOnly? from, DateOnly? to);

    Task<IReadOnlyList<StockMovementSnapshot>> ListByOriginAsync(string origin, string originCode);
}

// ───────────────── Inventory Monitoring: sales (owner: Lorena) ─────────────────

public record SaleSnapshot(string Code, DateOnly Date, string Status, decimal Total, DateTime CreatedAt);

public interface ISalesFacade
{
    Task<IReadOnlyList<SaleSnapshot>> ListAsync(DateOnly? from, DateOnly? to);
}

// ───────────────── Inventory Monitoring: suppliers and purchases (owner: Candy) ─────────────────

public record PurchaseSnapshot(string Code, DateOnly Date, string Status, decimal Total, DateTime CreatedAt);

public interface IProcurementFacade
{
    Task<IReadOnlyList<PurchaseSnapshot>> ListAsync(DateOnly? from, DateOnly? to);
}

// ───────────────── Alerts & Restocking (owner: Leo) ─────────────────

public interface IAlertsFacade
{
    /// <summary>R19: after a stock movement (low stock alert is created or resolved).</summary>
    Task OnStockChangedAsync(long productId, string movementType, int newRegisteredStock);

    /// <summary>R20: after a sensor reading (discrepancy alert is created or resolved).</summary>
    Task OnSensorReadingAsync(long productId, decimal weightKg);

    /// <summary>R21: a purchase created from an alert moves the restocking need to IN_PROGRESS.</summary>
    Task OnPurchaseRegisteredAsync(string purchaseCode, long? restockingNeedId);

    /// <summary>R21: receiving that purchase closes the cycle (alert resolved, need completed).</summary>
    Task OnPurchaseReceivedAsync(string purchaseCode, long? restockingNeedId);

    Task<int> CountActiveLowStockAsync();

    Task<long?> GetActiveDiscrepancyAlertIdAsync();
}

// ───────────────── External services (third-party): email and WhatsApp ─────────────────

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}

public interface IWhatsAppSender
{
    Task SendAsync(string toPhone, string text);
}
