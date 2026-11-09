
namespace SmartStock.Platform.API.ProductCatalog;

// JSON names are provided by WireNamingPolicy.
public record ProductRequest(
    string? Name,
    string? Sku,
    string? Category,
    decimal? UnitWeight,
    decimal? SalePrice,
    decimal? PurchaseCost,
    string? UsualSupplier,
    int? RegisteredStock,
    int? MinThreshold,
    int? MaxCapacity);

public record ThresholdRequest(int? MinThreshold);

/// <summary>
/// TS13: Product Catalog.
/// US05: Minimum threshold.
/// </summary>
[ApiController]
[Tags("Product Catalog")]
public class ProductsController(
    IProductCommandService commands,
    IProductQueryService queries) : ControllerBase
{
    [HttpGet("api/productos")]
    [EndpointSummary("Lists the products of the business")]
    [EndpointDescription(
        "Each product includes its sensor state and reference stock. Optional filter: sensorId.")]
    [ProducesResponseType(
        typeof(IReadOnlyList<ProductViewResource>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] long? sensorId)
    {
        var products = await queries.HandleAsync(
            new GetProductsQuery(sensorId));

        return Ok(products);
    }

    [HttpGet("api/productos/{id:long}")]
    [EndpointSummary("Gets one product")]
    [ProducesResponseType(
        typeof(ProductViewResource),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var product = await queries.HandleAsync(
            new GetProductByIdQuery(id));

        return Ok(product ?? throw DomainException.NotFound());
    }

    [HttpPost("api/productos")]
    [EndpointSummary("Registers a product")]
    [EndpointDescription(
        "Creates a product. Initial stock is registered through a movement (R17).")]
    [ProducesResponseType(
        typeof(ProductViewResource),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] ProductRequest request)
    {
        var id = await commands.HandleAsync(
            new CreateProductCommand(
                request.Name,
                request.Sku,
                request.Category,
                request.UnitWeight,
                request.SalePrice,
                request.PurchaseCost,
                request.UsualSupplier,
                request.RegisteredStock,
                request.MinThreshold,
                request.MaxCapacity));

        var product = await queries.HandleAsync(
            new GetProductByIdQuery(id));

        return StatusCode(
            StatusCodes.Status201Created,
            product);
    }

    [HttpPut("api/productos/{id:long}")]
    [EndpointSummary("Edits a product")]
    [EndpointDescription(
        "Updates product information without modifying registered stock (R17).")]
    [ProducesResponseType(
        typeof(ProductViewResource),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] ProductRequest request)
    {
        await commands.HandleAsync(
            new UpdateProductCommand(
                id,
                request.Name,
                request.Sku,
                request.Category,
                request.UnitWeight,
                request.SalePrice,
                request.PurchaseCost,
                request.UsualSupplier,
                request.MinThreshold,
                request.MaxCapacity));

        var product = await queries.HandleAsync(
            new GetProductByIdQuery(id));

        return Ok(product);
    }

    [HttpPut("api/sensores/{sensorId:long}/umbral")]
    [EndpointSummary(
        "Sets the minimum threshold of the product linked to a sensor")]
    [EndpointDescription(
        "Returns 422 when the threshold is invalid or exceeds maximum capacity.")]
    [ProducesResponseType(
        typeof(ProductViewResource),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetThreshold(
        long sensorId,
        [FromBody] ThresholdRequest request)
    {
        var productId = await commands.HandleAsync(
            new UpdateThresholdCommand(
                sensorId,
                request.MinThreshold));

        var product = await queries.HandleAsync(
            new GetProductByIdQuery(productId));

        return Ok(product);
    }
}
