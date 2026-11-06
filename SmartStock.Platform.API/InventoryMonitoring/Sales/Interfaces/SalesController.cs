namespace SmartStock.Platform.API.InventoryMonitoring.Sales;

public record SaleItemRequest(long? ProductId, int? Quantity, decimal? UnitPrice);

public record RegisterSaleRequest(IReadOnlyList<SaleItemRequest>? Items);

public record SaleItemResource(long ProductId, int Quantity, decimal UnitPrice, string ProductName, decimal Subtotal);

public record SaleResource(string Id, DateOnly Date, string Status, decimal Total, IReadOnlyList<SaleItemResource> Items);

public record SaleListResource(IReadOnlyList<SaleResource> Sales, decimal Total);

public record SaleDetailResource(
    string Id, DateOnly Date, string Status, decimal Total, IReadOnlyList<SaleItemResource> Items,
    IReadOnlyList<StockMovementResource> Movements);

/// <summary>TS07 and TS08 - sales.</summary>
[ApiController]
[Route("api/ventas")]
[Tags("Inventory Monitoring - Sales")]
public class SalesController(ISaleCommandService commands, ISaleQueryService queries) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registers a sale (TS07)")]
    [EndpointDescription("Creates one OUT stock movement per item (R17). 422 INSUFFICIENT_STOCK if a quantity is over the registered stock (R13); 422 NO_SALE_PRICE if a product has no sale price (R14). The stock is not changed when it fails.")]
    [ProducesResponseType(typeof(CreatedCodeResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register([FromBody] RegisterSaleRequest request)
    {
        var items = request.Items?.Select(i => new SaleItemInput(i.ProductId, i.Quantity, i.UnitPrice)).ToList();
        var code = await commands.HandleAsync(new RegisterSaleCommand(items));
        return StatusCode(StatusCodes.Status201Created, new CreatedCodeResource(code));
    }

    [HttpGet]
    [EndpointSummary("Lists the sales of a period (TS08)")]
    [EndpointDescription("Filters desde/hasta (yyyy-MM-dd). Returns the sales and the accumulated total of the COMPLETED ones.")]
    [ProducesResponseType(typeof(SaleListResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "desde")] DateOnly? from, [FromQuery(Name = "hasta")] DateOnly? to) =>
        Ok(await queries.HandleAsync(new ListSalesQuery(from, to)));

    [HttpGet("{code}")]
    [EndpointSummary("Gets the detail of a sale (TS08)")]
    [EndpointDescription("The code is the sale id, for example R-00041. Includes the stock movements it created.")]
    [ProducesResponseType(typeof(SaleDetailResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code) => Ok(await queries.HandleAsync(new GetSaleByCodeQuery(code)));
}
