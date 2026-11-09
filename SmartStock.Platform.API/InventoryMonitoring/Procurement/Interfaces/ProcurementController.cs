
namespace SmartStock.Platform.API.InventoryMonitoring.Procurement;

// JSON names come from WireNamingPolicy.
public record SupplierRequest(
    string? Name,
    string? Phone,
    string? ContactEmail);

public record SupplierResource(
    long Id,
    string Name,
    string ContactEmail,
    string Phone);

public record PurchaseItemRequest(
    long? ProductId,
    int? Quantity,
    decimal? UnitCost);

public record RegisterPurchaseRequest(
    long? SupplierId,
    long? RestockingNeedId,
    DateOnly? Date,
    IReadOnlyList<PurchaseItemRequest>? Items);

public record PurchaseItemResource(
    long ProductId,
    int Quantity,
    decimal UnitCost,
    string ProductName,
    decimal Subtotal);

public record PurchaseResource(
    string Id,
    long SupplierId,
    string SupplierName,
    DateOnly Date,
    string Status,
    decimal Total,
    long? RestockingNeedId,
    IReadOnlyList<PurchaseItemResource> Items);

public record PurchaseListResource(
    IReadOnlyList<PurchaseResource> Purchases,
    decimal Total);

public record PurchaseDetailResource(
    string Id,
    long SupplierId,
    string SupplierName,
    DateOnly Date,
    string Status,
    decimal Total,
    long? RestockingNeedId,
    IReadOnlyList<PurchaseItemResource> Items,
    IReadOnlyList<StockMovementResource> Movements);

/// <summary>
/// TS09 - Suppliers.
/// </summary>
[ApiController]
[Route("api/proveedores")]
[Tags("Inventory Monitoring - Suppliers")]
public class SuppliersController(
    ISupplierService service) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registers a supplier (TS09)")]
    [EndpointDescription(
        "400 INCOMPLETE if name or phone is missing; " +
        "409 DUPLICATE_SUPPLIER if the same name and phone already exist (R8).")]
    [ProducesResponseType(
        typeof(SupplierResource),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] SupplierRequest request)
    {
        var result = await service.HandleAsync(
            new RegisterSupplierCommand(
                request.Name,
                request.Phone,
                request.ContactEmail));

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpGet]
    [EndpointSummary("Lists the suppliers (TS09)")]
    [ProducesResponseType(
        typeof(IReadOnlyList<SupplierResource>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await service.ListAsync());
    }
}

/// <summary>
/// TS10, TS11 and TS12 - Purchases.
/// </summary>
[ApiController]
[Route("api/compras")]
[Tags("Inventory Monitoring - Purchases")]
public class PurchasesController(
    IPurchaseCommandService commands,
    IPurchaseQueryService queries) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registers a purchase (TS10)")]
    [EndpointDescription(
        "The purchase starts PENDING and does not change stock (R15). " +
        "It can come from a low stock alert (restockingNeedId, R21). " +
        "404 SUPPLIER_NOT_FOUND if supplier does not exist.")]
    [ProducesResponseType(
        typeof(CreatedCodeResource),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPurchaseRequest request)
    {
        var items = request.Items?
            .Select(item => new PurchaseItemInput(
                item.ProductId,
                item.Quantity,
                item.UnitCost))
            .ToList();

        var code = await commands.HandleAsync(
            new RegisterPurchaseCommand(
                request.SupplierId,
                request.RestockingNeedId,
                request.Date,
                items));

        return StatusCode(
            StatusCodes.Status201Created,
            new CreatedCodeResource(code));
    }

    [HttpGet]
    [EndpointSummary("Lists the purchases of a period (TS12)")]
    [EndpointDescription(
        "Filters desde/hasta (yyyy-MM-dd). " +
        "Returns purchases and accumulated total.")]
    [ProducesResponseType(
        typeof(PurchaseListResource),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "desde")] DateOnly? from,
        [FromQuery(Name = "hasta")] DateOnly? to)
    {
        return Ok(await queries.HandleAsync(
            new ListPurchasesQuery(from, to)));
    }

    [HttpGet("{code}")]
    [EndpointSummary("Gets the detail of a purchase (TS12)")]
    [EndpointDescription(
        "The code is the purchase ID, for example PO-00031. " +
        "Includes the stock movements it created.")]
    [ProducesResponseType(
        typeof(PurchaseDetailResource),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code)
    {
        return Ok(await queries.HandleAsync(
            new GetPurchaseByCodeQuery(code)));
    }

    [HttpPatch("{code}/recepcion")]
    [EndpointSummary("Receives a purchase (TS11)")]
    [EndpointDescription(
        "Adds quantities to stock with IN movements (R16). " +
        "409 ALREADY_RECEIVED prevents receiving twice.")]
    [ProducesResponseType(
        typeof(PurchaseResource),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(string code)
    {
        return Ok(await commands.HandleAsync(
            new ReceivePurchaseCommand(code)));
    }
}
