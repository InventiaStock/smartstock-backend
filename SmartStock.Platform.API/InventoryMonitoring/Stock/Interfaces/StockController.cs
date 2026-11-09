namespace SmartStock.Platform.API.InventoryMonitoring.Stock;

/// <summary>TS02, TS06, TS15 and the product detail (stock, comparison and stock movements).</summary>
[ApiController]
[Tags("Inventory Monitoring - Stock")]
public class StockController(IStockQueryService queries) : ControllerBase
{
    [HttpGet("api/productos/{id:long}/stock")]
    [EndpointSummary("Gets the stock level of a product (TS02)")]
    [EndpointDescription("Registered stock, reference stock (physical when the sensor is online) and level lowStock/healthy.")]
    [ProducesResponseType(typeof(StockLevelResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStock(long id) => Ok(await queries.HandleAsync(new GetStockQuery(id)));

    [HttpGet("api/productos/{id:long}/detalle")]
    [EndpointSummary("Gets a product with its sensor and its last 5 readings")]
    [ProducesResponseType(typeof(ProductDetailResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(long id) => Ok(await queries.HandleAsync(new GetProductDetailQuery(id)));

    [HttpGet("api/inventario/comparacion")]
    [EndpointSummary("Compares physical and registered stock of all products (TS06)")]
    [EndpointDescription("Result: MATCH, DISCREPANCY (difference over 10 %, R20) or REGISTERED_ONLY (no online sensor).")]
    [ProducesResponseType(typeof(ComparisonResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComparison() => Ok(await queries.GetComparisonAsync());

    [HttpGet("api/inventario/comparacion/{productId:long}")]
    [EndpointSummary("Compares physical and registered stock of one product (TS06)")]
    [EndpointDescription("409 NO_REGISTERED_QUANTITY if the product has no registered quantity.")]
    [ProducesResponseType(typeof(ComparisonItemResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetComparison(long productId) =>
        Ok(await queries.HandleAsync(new GetComparisonQuery(productId)));

    [HttpGet("api/movimientos-stock")]
    [EndpointSummary("Lists the stock movements of a period (TS15)")]
    [EndpointDescription("Filters desde/hasta (yyyy-MM-dd). An empty period returns an empty list with 200.")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovements(
        [FromQuery(Name = "desde")] DateOnly? from, [FromQuery(Name = "hasta")] DateOnly? to) =>
        Ok(await queries.HandleAsync(new ListMovementsQuery(from, to)));
}
