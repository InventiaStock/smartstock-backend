
namespace SmartStock.Platform.API.InventoryMonitoring.Procurement;

public record RegisterSupplierCommand(
    string? Name, string? Phone, string? Email);

public record PurchaseItemInput(
    long? ProductId, int? Quantity, decimal? UnitCost);

public record RegisterPurchaseCommand(
    long? SupplierId, long? RestockingNeedId, DateOnly? Date,
    IReadOnlyList<PurchaseItemInput>? Items);

public record ReceivePurchaseCommand(string Code);

public record ListPurchasesQuery(DateOnly? From, DateOnly? To);

public record GetPurchaseByCodeQuery(string Code);

public interface ISupplierService
{
    /// <summary>
    /// TS09: registers a supplier.
    /// R8: 409 DUPLICATE_SUPPLIER if name + phone already exist.
    /// </summary>
    Task<SupplierResource> HandleAsync(RegisterSupplierCommand command);

    Task<IReadOnlyList<SupplierResource>> ListAsync();
}

public interface IPurchaseCommandService
{
    /// <summary>
    /// TS10: registers a PENDING purchase.
    /// Returns the code (PO-00032).
    /// </summary>
    Task<string> HandleAsync(RegisterPurchaseCommand command);

    /// <summary>
    /// TS11: receives the purchase and adds the stock (R16).
    /// </summary>
    Task<PurchaseResource> HandleAsync(ReceivePurchaseCommand command);
}

public interface IPurchaseQueryService
{
    Task<PurchaseListResource> HandleAsync(ListPurchasesQuery query);

    Task<PurchaseDetailResource> HandleAsync(GetPurchaseByCodeQuery query);
}

public class SupplierService(
    ISupplierRepository suppliers,
    IUnitOfWork unitOfWork) : ISupplierService
{
    public async Task<SupplierResource> HandleAsync(
        RegisterSupplierCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name) ||
            string.IsNullOrWhiteSpace(command.Phone))
            throw DomainException.BadRequest("INCOMPLETE");

        var existing = await suppliers.ListAsync();

        if (existing.Any(supplier =>
                supplier.IsSameAs(command.Name, command.Phone)))
            throw DomainException.Conflict("DUPLICATE_SUPPLIER");

        var created = new Supplier(
            command.Name,
            command.Phone,
            command.Email);

        await suppliers.AddAsync(created);
        await unitOfWork.CompleteAsync();

        return ToResource(created);
    }

    public async Task<IReadOnlyList<SupplierResource>> ListAsync() =>
        (await suppliers.ListAsync())
        .Select(ToResource)
        .ToList();

    private static SupplierResource ToResource(Supplier supplier) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.Email,
            supplier.Phone);
}

public class PurchaseCommandService(
    IPurchaseRepository purchases,
    ISupplierRepository suppliers,
    IProductCatalogFacade catalog,
    IStockMovementFacade movements,
    IAlertsFacade alerts,
    IUnitOfWork unitOfWork) : IPurchaseCommandService
{
    public async Task<string> HandleAsync(
        RegisterPurchaseCommand command)
    {
        var inputs = command.Items ?? [];

        if (inputs.Count == 0)
            throw DomainException.BadRequest("NO_ITEMS");

        var supplier = command.SupplierId is { } supplierId
            ? await suppliers.FindByIdAsync(supplierId)
            : null;

        if (supplier is null)
            throw DomainException.NotFound("SUPPLIER_NOT_FOUND");

        var items = new List<PurchaseItem>();

        foreach (var input in inputs)
        {
            var product = input.ProductId is { } id
                ? await catalog.FindByIdAsync(id)
                : null;

            if (product is null)
                throw DomainException.NotFound("PRODUCT_NOT_FOUND");

            if (input.Quantity is not > 0)
                throw DomainException.BadRequest("INVALID_QUANTITY");

            items.Add(new PurchaseItem(
                product.Id,
                input.Quantity.Value,
                input.UnitCost ?? 0));
        }

        var purchase = new Purchase(
            await purchases.NextNumberAsync(),
            supplier.Id,
            command.Date ?? Clock.Today,
            command.RestockingNeedId,
            items);

        await purchases.AddAsync(purchase);

        // R15: PENDING, the stock does not change.
        // R21: an alert-based need moves to IN_PROGRESS.
        await alerts.OnPurchaseRegisteredAsync(
            purchase.Code,
            purchase.RestockingNeedId);

        await unitOfWork.CompleteAsync();

        return purchase.Code;
    }

    public async Task<PurchaseResource> HandleAsync(
        ReceivePurchaseCommand command)
    {
        var purchase = await purchases.FindByCodeAsync(command.Code)
            ?? throw DomainException.NotFound();

        // R16: prevents receiving the same purchase twice.
        purchase.MarkReceived();

        // One IN movement per item.
        foreach (var item in purchase.Items)
        {
            await movements.RegisterMovementAsync(
                new RegisterMovementRequest(
                    item.ProductId,
                    MovementTypes.In,
                    MovementOrigins.Purchase,
                    purchase.Code,
                    item.Quantity));
        }

        // R21: closes the alert cycle.
        await alerts.OnPurchaseReceivedAsync(
            purchase.Code,
            purchase.RestockingNeedId);

        await unitOfWork.CompleteAsync();

        var supplier = await suppliers.FindByIdAsync(
            purchase.SupplierId);

        return PurchaseMapper.ToResource(
            purchase,
            supplier?.Name ?? string.Empty,
            null);
    }
}

public class PurchaseQueryService(
    IPurchaseRepository purchases,
    ISupplierRepository suppliers,
    IProductCatalogFacade catalog,
    IStockMovementFacade movements) : IPurchaseQueryService
{
    public async Task<PurchaseListResource> HandleAsync(
        ListPurchasesQuery query)
    {
        var list = await purchases.ListByDateRangeAsync(
            query.From,
            query.To);

        var supplierNames = await SupplierNamesAsync();
        var productNames = await ProductNamesAsync();

        var resources = list
            .Select(p => PurchaseMapper.ToResource(
                p,
                supplierNames.GetValueOrDefault(
                    p.SupplierId,
                    string.Empty),
                productNames))
            .ToList();

        var total = InventoryRules.Money(
            list.Where(p => p.Status != PurchaseStatuses.Cancelled)
                .Sum(p => p.Total));

        return new PurchaseListResource(resources, total);
    }

    public async Task<PurchaseDetailResource> HandleAsync(
        GetPurchaseByCodeQuery query)
    {
        var purchase = await purchases.FindByCodeAsync(query.Code)
            ?? throw DomainException.NotFound();

        var supplierNames = await SupplierNamesAsync();
        var productNames = await ProductNamesAsync();

        var purchaseMovements =
            await movements.ListByOriginAsync(
                MovementOrigins.Purchase,
                purchase.Code);

        var summary = PurchaseMapper.ToResource(
            purchase,
            supplierNames.GetValueOrDefault(
                purchase.SupplierId,
                string.Empty),
            productNames);

        return new PurchaseDetailResource(
            summary.Id,
            summary.SupplierId,
            summary.SupplierName,
            summary.Date,
            summary.Status,
            summary.Total,
            summary.RestockingNeedId,
            summary.Items,
            purchaseMovements
                .Select(StockMovementResource.From)
                .ToList());
    }

    private async Task<Dictionary<long, string>> SupplierNamesAsync() =>
        (await suppliers.ListAsync())
        .ToDictionary(
            supplier => supplier.Id,
            supplier => supplier.Name);

    private async Task<Dictionary<long, string>> ProductNamesAsync() =>
        (await catalog.ListAsync())
        .ToDictionary(
            product => product.Id,
            product => product.Name);
}

public static class PurchaseMapper
{
    public static PurchaseResource ToResource(
        Purchase purchase,
        string supplierName,
        Dictionary<long, string>? productNames) => new(
            purchase.Code,
            purchase.SupplierId,
            supplierName,
            purchase.Date,
            purchase.Status,
            purchase.Total,
            purchase.RestockingNeedId,
            purchase.Items.Select(item =>
                new PurchaseItemResource(
                    item.ProductId,
                    item.Quantity,
                    item.UnitCost,
                    productNames?.GetValueOrDefault(
                        item.ProductId,
                        string.Empty) ?? string.Empty,
                    InventoryRules.Money(
                        item.Quantity * item.UnitCost)))
                .ToList());
}

/// <summary>
/// What other contexts can ask Procurement.
/// See IProcurementFacade.
/// </summary>
public class ProcurementFacade(
    IPurchaseRepository purchases) : IProcurementFacade
{
    public async Task<IReadOnlyList<PurchaseSnapshot>> ListAsync(
        DateOnly? from,
        DateOnly? to) =>
        (await purchases.ListByDateRangeAsync(from, to))
        .Select(p => new PurchaseSnapshot(
            p.Code,
            p.Date,
            p.Status,
            p.Total,
            p.CreatedAt))
        .ToList();
}
