
namespace SmartStock.Platform.API.ProductCatalog;

public record CreateProductCommand(
    string? Name, string? Sku, string? Category, decimal? UnitWeight, decimal? SalePrice, decimal? PurchaseCost,
    string? UsualSupplier, int? InitialStock, int? MinThreshold, int? MaxCapacity);

public record UpdateProductCommand(
    long ProductId, string? Name, string? Sku, string? Category, decimal? UnitWeight, decimal? SalePrice,
    decimal? PurchaseCost, string? UsualSupplier, int? MinThreshold, int? MaxCapacity);

public record UpdateThresholdCommand(long SensorId, int? MinThreshold);

public record GetProductsQuery(long? SensorId);

public record GetProductByIdQuery(long ProductId);

public interface IProductCommandService
{
    /// <summary>Creates the product and returns its id.</summary>
    Task<long> HandleAsync(CreateProductCommand command);

    Task<long> HandleAsync(UpdateProductCommand command);

    /// <summary>US05: changes the minimum threshold of the product linked to the sensor.</summary>
    Task<long> HandleAsync(UpdateThresholdCommand command);
}

public interface IProductQueryService
{
    Task<IReadOnlyList<ProductViewResource>> HandleAsync(GetProductsQuery query);

    Task<ProductViewResource?> HandleAsync(GetProductByIdQuery query);
}

public static class ProductSnapshotMapper
{
    public static ProductSnapshot ToSnapshot(Product p) => new(
        p.Id, p.Name, p.Sku, p.Category, p.UnitWeightKg, p.SalePrice, p.PurchaseCost,
        p.UsualSupplier, p.RegisteredStock, p.MinThreshold, p.MaxCapacity);
}

public class ProductCommandService(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IIoTDeviceFacade iot,
    IStockMovementFacade movements) : IProductCommandService
{
    public async Task<long> HandleAsync(CreateProductCommand command)
    {
        var maxCapacity = command.MaxCapacity ?? 100;

        var errors = Validate(command.Name, command.Category, command.UnitWeight, command.SalePrice,
            command.PurchaseCost, command.MinThreshold, maxCapacity);
        if (errors.Count > 0)
            throw DomainException.BadRequest("INVALID_PRODUCT", DomainException.With(("errors", errors)));

        var product = new Product(
            command.Name!.Trim(), command.Sku?.Trim() ?? string.Empty, command.Category!.Trim(),
            command.UnitWeight!.Value, command.SalePrice, command.PurchaseCost,
            command.UsualSupplier?.Trim() ?? string.Empty, command.MinThreshold!.Value, maxCapacity);

        await products.AddAsync(product);
        await unitOfWork.CompleteAsync();

        // R17: an initial stock is also a movement.
        if (command.InitialStock is > 0)
        {
            await movements.RegisterMovementAsync(new RegisterMovementRequest(
                product.Id, MovementTypes.In, MovementOrigins.Adjustment, "INITIAL", command.InitialStock.Value));
            await unitOfWork.CompleteAsync();
        }

        return product.Id;
    }

    public async Task<long> HandleAsync(UpdateProductCommand command)
    {
        var product = await products.FindByIdAsync(command.ProductId) ?? throw DomainException.NotFound();

        // Only the fields that came in the request change.
        var name = command.Name ?? product.Name;
        var category = command.Category ?? product.Category;
        var unitWeight = command.UnitWeight ?? product.UnitWeightKg;
        var salePrice = command.SalePrice ?? product.SalePrice;
        var purchaseCost = command.PurchaseCost ?? product.PurchaseCost;
        var minThreshold = command.MinThreshold ?? product.MinThreshold;
        var maxCapacity = command.MaxCapacity ?? product.MaxCapacity;

        var errors = Validate(name, category, unitWeight, salePrice, purchaseCost, minThreshold, maxCapacity);
        if (errors.Count > 0)
            throw DomainException.BadRequest("INVALID_PRODUCT", DomainException.With(("errors", errors)));

        // Editing never changes the stock or the sensor link (R17).
        product.UpdateDetails(name.Trim(), command.Sku?.Trim() ?? product.Sku, category.Trim(), unitWeight,
            salePrice, purchaseCost, command.UsualSupplier?.Trim() ?? product.UsualSupplier, minThreshold, maxCapacity);
        await unitOfWork.CompleteAsync();

        return product.Id;
    }

    public async Task<long> HandleAsync(UpdateThresholdCommand command)
    {
        var sensor = await iot.FindByIdAsync(command.SensorId);
        var product = sensor?.ProductId is { } productId ? await products.FindByIdAsync(productId) : null;
        if (product is null) throw DomainException.NotFound();

        if (command.MinThreshold is not > 0) throw DomainException.Unprocessable("THRESHOLD_INVALID");

        // R7: the threshold never exceeds the maximum capacity.
        if (command.MinThreshold > product.MaxCapacity)
        {
            throw DomainException.Unprocessable("THRESHOLD_EXCEEDS_CAPACITY",
                DomainException.With(("maxCapacity", product.MaxCapacity)));
        }

        product.ChangeMinThreshold(command.MinThreshold.Value);
        await unitOfWork.CompleteAsync();

        return product.Id;
    }

    // R5 and R7: the same checks as the Fake API, with the same error keys.
    private static Dictionary<string, string> Validate(
        string? name, string? category, decimal? unitWeight, decimal? salePrice, decimal? purchaseCost,
        int? minThreshold, int maxCapacity)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(name)) errors["nombre"] = "REQUIRED";
        if (string.IsNullOrWhiteSpace(category)) errors["categoria"] = "REQUIRED";
        if (unitWeight is not > 0) errors["pesoUnitario"] = "GREATER_THAN_ZERO";
        if ((salePrice ?? 0) < 0 || (purchaseCost ?? 0) < 0) errors["precio"] = "NEGATIVE";
        if (minThreshold is not > 0 || minThreshold > maxCapacity) errors["umbralMinimo"] = "INVALID_THRESHOLD";

        return errors;
    }
}

public class ProductQueryService(IProductRepository products, IIoTDeviceFacade iot) : IProductQueryService
{
    public async Task<IReadOnlyList<ProductViewResource>> HandleAsync(GetProductsQuery query)
    {
        var list = await products.ListAsync();
        var sensorsByProduct = await SensorsByProductAsync();

        return list
            .Select(product => ProductViewFactory.Create(
                ProductSnapshotMapper.ToSnapshot(product),
                sensorsByProduct.GetValueOrDefault(product.Id)))
            .Where(view => query.SensorId is null || view.SensorId == query.SensorId)
            .ToList();
    }

    public async Task<ProductViewResource?> HandleAsync(GetProductByIdQuery query)
    {
        var product = await products.FindByIdAsync(query.ProductId);
        if (product is null) return null;

        var sensor = await iot.FindByProductIdAsync(product.Id);
        return ProductViewFactory.Create(ProductSnapshotMapper.ToSnapshot(product), sensor);
    }

    private async Task<Dictionary<long, SensorSnapshot>> SensorsByProductAsync()
    {
        var sensors = await iot.ListAsync();
        return sensors
            .Where(sensor => sensor.ProductId is not null)
            .ToDictionary(sensor => sensor.ProductId!.Value);
    }
}

/// <summary>What the other contexts can ask the catalog (see IProductCatalogFacade).</summary>
public class ProductCatalogFacade(IProductRepository products) : IProductCatalogFacade
{
    public async Task<ProductSnapshot?> FindByIdAsync(long productId)
    {
        var product = await products.FindByIdAsync(productId);
        return product is null ? null : ProductSnapshotMapper.ToSnapshot(product);
    }

    public async Task<IReadOnlyList<ProductSnapshot>> ListAsync() =>
        (await products.ListAsync()).Select(ProductSnapshotMapper.ToSnapshot).ToList();

    public async Task<int> ChangeRegisteredStockAsync(long productId, int delta)
    {
        var product = await products.FindByIdAsync(productId)
            ?? throw DomainException.NotFound("PRODUCT_NOT_FOUND", DomainException.With(("productoId", productId)));
        return product.ChangeRegisteredStock(delta);
    }
}
