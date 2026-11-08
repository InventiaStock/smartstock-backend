namespace SmartStock.Platform.API.InventoryMonitoring.Stock;

// Resources returned by the Stock endpoints. The JSON names in Spanish come from WireNamingPolicy.
// (Recursos que devuelven los endpoints de Stock: los nombres JSON salen de WireNamingPolicy.)

public record StockLevelResource(
    long ProductId, int RegisteredStock, int ReferenceStock, string StockLevel, string SensorStatus, int? SensorUnits);

public record DetailSensorResource(
    long Id, string Code, string Status, decimal WeightKg, int Units, DateTime? LastReadingAt);

public record DetailReadingResource(long Id, long SensorId, decimal WeightKg, DateTime ReadAt, int Units);

public record ProductDetailResource(
    ProductViewResource Product, DetailSensorResource? Sensor, IReadOnlyList<DetailReadingResource> Readings);

public record ComparisonItemResource(
    long ProductId, string ProductName, int RegisteredStock, decimal? WeightKg, int? PhysicalUnits,
    double? DifferencePct, string Result);

public record ComparisonResource(IReadOnlyList<ComparisonItemResource> Items, int Discrepancies, long? AlertId);

public record GetStockQuery(long ProductId);

public record GetProductDetailQuery(long ProductId);

public record GetComparisonQuery(long ProductId);

public record ListMovementsQuery(DateOnly? From, DateOnly? To);

public interface IStockQueryService
{
    Task<StockLevelResource> HandleAsync(GetStockQuery query);

    Task<ProductDetailResource> HandleAsync(GetProductDetailQuery query);

    Task<ComparisonResource> GetComparisonAsync();

    Task<ComparisonItemResource> HandleAsync(GetComparisonQuery query);

    Task<IReadOnlyList<StockMovementResource>> HandleAsync(ListMovementsQuery query);
}

/// <summary>
/// TS14 engine. Sales, purchases and adjustments call RegisterMovementAsync; it changes the stock through
/// the catalog and asks Alerts to evaluate R19. It does not save: the caller saves everything in one transaction.
/// (Motor TS14: no guarda; quien llama guarda todo junto.)
/// </summary>
public class StockMovementFacade(
    IStockMovementRepository movements,
    IProductCatalogFacade catalog,
    IAlertsFacade alerts) : IStockMovementFacade
{
    public async Task RegisterMovementAsync(RegisterMovementRequest request)
    {
        if (request.Quantity <= 0)
            throw DomainException.BadRequest("INVALID_QUANTITY", DomainException.With(("productoId", request.ProductId)));

        var delta = request.Type == MovementTypes.Out ? -request.Quantity : request.Quantity;
        var newStock = await catalog.ChangeRegisteredStockAsync(request.ProductId, delta);

        await movements.AddAsync(new StockMovement(
            request.ProductId, request.Type, request.Origin, request.OriginCode, request.Quantity, Clock.Today));

        await alerts.OnStockChangedAsync(request.ProductId, request.Type, newStock);
    }

    public async Task<IReadOnlyList<StockMovementSnapshot>> ListAsync(DateOnly? from, DateOnly? to) =>
        await ToSnapshotsAsync(await movements.ListByDateRangeAsync(from, to));

    public async Task<IReadOnlyList<StockMovementSnapshot>> ListByOriginAsync(string origin, string originCode) =>
        await ToSnapshotsAsync(await movements.ListByOriginAsync(origin, originCode));

    private async Task<IReadOnlyList<StockMovementSnapshot>> ToSnapshotsAsync(IReadOnlyList<StockMovement> list)
    {
        var names = (await catalog.ListAsync()).ToDictionary(product => product.Id, product => product.Name);
        return list
            .Select(m => new StockMovementSnapshot(
                m.Id, m.ProductId, names.GetValueOrDefault(m.ProductId, string.Empty),
                m.Type, m.Origin, m.OriginCode, m.Quantity, m.Date))
            .ToList();
    }
}

public class StockQueryService(
    IProductCatalogFacade catalog,
    IIoTDeviceFacade iot,
    IAlertsFacade alerts,
    IStockMovementFacade movements) : IStockQueryService
{
    // TS02
    public async Task<StockLevelResource> HandleAsync(GetStockQuery query)
    {
        var (product, sensor) = await LoadAsync(query.ProductId);
        var view = ProductViewFactory.Create(product, sensor);

        return new StockLevelResource(
            product.Id, product.RegisteredStock, view.ReferenceStock, view.StockLevel, view.SensorStatus, view.SensorUnits);
    }

    // Product detail: product + sensor + last 5 readings
    public async Task<ProductDetailResource> HandleAsync(GetProductDetailQuery query)
    {
        var (product, sensor) = await LoadAsync(query.ProductId);

        DetailSensorResource? sensorResource = null;
        var readings = new List<DetailReadingResource>();

        if (sensor is not null)
        {
            sensorResource = new DetailSensorResource(
                sensor.Id, sensor.Code, sensor.Status, sensor.WeightKg,
                InventoryRules.ToUnits(sensor.WeightKg, product.UnitWeightKg), sensor.LastReadingAt);

            readings = (await iot.GetLatestReadingsAsync(sensor.Id, 5))
                .Select(r => new DetailReadingResource(
                    r.Id, r.SensorId, r.WeightKg, r.ReadAt, InventoryRules.ToUnits(r.WeightKg, product.UnitWeightKg)))
                .ToList();
        }

        return new ProductDetailResource(ProductViewFactory.Create(product, sensor), sensorResource, readings);
    }

    // TS06 (list for the comparison screen)
    public async Task<ComparisonResource> GetComparisonAsync()
    {
        var products = await catalog.ListAsync();
        var sensorsByProduct = (await iot.ListAsync())
            .Where(sensor => sensor.ProductId is not null)
            .ToDictionary(sensor => sensor.ProductId!.Value);

        var items = products.Select(p => Compare(p, sensorsByProduct.GetValueOrDefault(p.Id))).ToList();

        return new ComparisonResource(
            items, items.Count(i => i.Result == "DISCREPANCY"), await alerts.GetActiveDiscrepancyAlertIdAsync());
    }

    // TS06 (one product)
    public async Task<ComparisonItemResource> HandleAsync(GetComparisonQuery query)
    {
        var (product, sensor) = await LoadAsync(query.ProductId);

        // TS06 scenario 2: no registered quantity, the comparison cannot be calculated
        if (product.RegisteredStock == 0) throw DomainException.Conflict("NO_REGISTERED_QUANTITY");

        return Compare(product, sensor);
    }

    // TS15
    public async Task<IReadOnlyList<StockMovementResource>> HandleAsync(ListMovementsQuery query) =>
        (await movements.ListAsync(query.From, query.To)).Select(StockMovementResource.From).ToList();

    private async Task<(ProductSnapshot Product, SensorSnapshot? Sensor)> LoadAsync(long productId)
    {
        var product = await catalog.FindByIdAsync(productId) ?? throw DomainException.NotFound();
        return (product, await iot.FindByProductIdAsync(productId));
    }

    // R17/R18: the sensor only verifies. R20: a difference over 10 % is a DISCREPANCY.
    private static ComparisonItemResource Compare(ProductSnapshot product, SensorSnapshot? sensor)
    {
        if (sensor is not { Status: SensorStatuses.Online })
            return new ComparisonItemResource(product.Id, product.Name, product.RegisteredStock, null, null, null, "REGISTERED_ONLY");

        var physical = InventoryRules.ToUnits(sensor.WeightKg, product.UnitWeightKg);
        var pct = InventoryRules.RawDifferencePct(physical, product.RegisteredStock);

        return new ComparisonItemResource(
            product.Id, product.Name, product.RegisteredStock, sensor.WeightKg, physical, Math.Round(pct, 1),
            Math.Abs(pct) > InventoryRules.DiscrepancyThresholdPct ? "DISCREPANCY" : "MATCH");
    }
}
