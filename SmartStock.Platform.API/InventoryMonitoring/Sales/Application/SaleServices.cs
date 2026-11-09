namespace SmartStock.Platform.API.InventoryMonitoring.Sales;

public record SaleItemInput(long? ProductId, int? Quantity, decimal? UnitPrice);

public record RegisterSaleCommand(IReadOnlyList<SaleItemInput>? Items);

public record ListSalesQuery(DateOnly? From, DateOnly? To);

public record GetSaleByCodeQuery(string Code);

public interface ISaleCommandService
{
    /// <summary>TS07: registers the sale and its OUT movements. Returns the code (R-00042).</summary>
    Task<string> HandleAsync(RegisterSaleCommand command);
}

public interface ISaleQueryService
{
    Task<SaleListResource> HandleAsync(ListSalesQuery query);

    Task<SaleDetailResource> HandleAsync(GetSaleByCodeQuery query);
}

public class SaleCommandService(
    ISaleRepository sales,
    IProductCatalogFacade catalog,
    IStockMovementFacade movements,
    IUnitOfWork unitOfWork) : ISaleCommandService
{
    public async Task<string> HandleAsync(RegisterSaleCommand command)
    {
        var inputs = command.Items ?? [];
        if (inputs.Count == 0) throw DomainException.BadRequest("NO_ITEMS");

        var items = new List<SaleItem>();
        var wantedByProduct = new Dictionary<long, int>();
        var productsById = new Dictionary<long, ProductSnapshot>();

        foreach (var input in inputs)
        {
            var product = input.ProductId is { } id ? await catalog.FindByIdAsync(id) : null;
            if (product is null)
                throw DomainException.NotFound("PRODUCT_NOT_FOUND", DomainException.With(("productoId", input.ProductId)));

            // R14: a product without sale price cannot be sold (producto sin precio no se vende)
            if (product.SalePrice is not > 0 || input.UnitPrice is not > 0)
            {
                throw DomainException.Unprocessable("NO_SALE_PRICE",
                    DomainException.With(("productoId", product.Id), ("productoNombre", product.Name)));
            }

            if (input.Quantity is not > 0)
                throw DomainException.BadRequest("INVALID_QUANTITY", DomainException.With(("productoId", product.Id)));

            productsById[product.Id] = product;
            wantedByProduct[product.Id] = wantedByProduct.GetValueOrDefault(product.Id) + input.Quantity.Value;
            items.Add(new SaleItem(product.Id, input.Quantity.Value, input.UnitPrice.Value));
        }

        // R13: a sale never exceeds the registered stock; if it fails nothing changes
        foreach (var (productId, wanted) in wantedByProduct)
        {
            var product = productsById[productId];
            if (wanted > product.RegisteredStock)
            {
                throw DomainException.Unprocessable("INSUFFICIENT_STOCK", DomainException.With(
                    ("productoId", product.Id), ("productoNombre", product.Name), ("available", product.RegisteredStock)));
            }
        }

        var sale = new Sale(await sales.NextNumberAsync(), Clock.Today, items);
        await sales.AddAsync(sale);

        // R17: one OUT movement per item (TS14). Everything is saved together below.
        foreach (var item in items)
        {
            await movements.RegisterMovementAsync(new RegisterMovementRequest(
                item.ProductId, MovementTypes.Out, MovementOrigins.Sale, sale.Code, item.Quantity));
        }

        await unitOfWork.CompleteAsync();
        return sale.Code;
    }
}

public class SaleQueryService(
    ISaleRepository sales,
    IProductCatalogFacade catalog,
    IStockMovementFacade movements) : ISaleQueryService
{
    public async Task<SaleListResource> HandleAsync(ListSalesQuery query)
    {
        var list = await sales.ListByDateRangeAsync(query.From, query.To);
        var names = await ProductNamesAsync();

        var resources = list.Select(sale => ToResource(sale, names)).ToList();
        var total = InventoryRules.Money(list.Where(s => s.Status == SaleStatuses.Completed).Sum(s => s.Total));

        return new SaleListResource(resources, total);
    }

    public async Task<SaleDetailResource> HandleAsync(GetSaleByCodeQuery query)
    {
        var sale = await sales.FindByCodeAsync(query.Code) ?? throw DomainException.NotFound();
        var names = await ProductNamesAsync();
        var saleMovements = await movements.ListByOriginAsync(MovementOrigins.Sale, sale.Code);

        return new SaleDetailResource(
            sale.Code, sale.Date, sale.Status, sale.Total,
            sale.Items.Select(item => ToItemResource(item, names)).ToList(),
            saleMovements.Select(StockMovementResource.From).ToList());
    }

    private async Task<Dictionary<long, string>> ProductNamesAsync() =>
        (await catalog.ListAsync()).ToDictionary(product => product.Id, product => product.Name);

    private static SaleResource ToResource(Sale sale, Dictionary<long, string> names) => new(
        sale.Code, sale.Date, sale.Status, sale.Total, sale.Items.Select(item => ToItemResource(item, names)).ToList());

    private static SaleItemResource ToItemResource(SaleItem item, Dictionary<long, string> names) => new(
        item.ProductId, item.Quantity, item.UnitPrice, names.GetValueOrDefault(item.ProductId, string.Empty),
        InventoryRules.Money(item.Quantity * item.UnitPrice));
}

/// <summary>What the other contexts can ask Sales (see ISalesFacade).</summary>
public class SalesFacade(ISaleRepository sales) : ISalesFacade
{
    public async Task<IReadOnlyList<SaleSnapshot>> ListAsync(DateOnly? from, DateOnly? to) =>
        (await sales.ListByDateRangeAsync(from, to))
        .Select(sale => new SaleSnapshot(sale.Code, sale.Date, sale.Status, sale.Total, sale.CreatedAt))
        .ToList();
}
