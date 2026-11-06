namespace SmartStock.Platform.API.IoTDevice;

public record LinkSensorCommand(long? SensorId, long? ProductId);

public record ReceiveReadingCommand(long SensorId, decimal? Weight, DateTime? ReadAt);

public record GetSensorByIdQuery(long SensorId);

public interface ISensorCommandService
{
    /// <summary>US04: links a sensor to a product. Returns the sensor id.</summary>
    Task<long> HandleAsync(LinkSensorCommand command);

    /// <summary>TS01: stores a reading sent by the microcontroller. Returns the reading id.</summary>
    Task<long> HandleAsync(ReceiveReadingCommand command);
}

public interface ISensorQueryService
{
    Task<IReadOnlyList<SensorResource>> ListAsync();

    Task<SensorResource?> HandleAsync(GetSensorByIdQuery query);

    Task<SensorStatusResource?> GetStatusAsync(long sensorId);
}

public class SensorCommandService(
    ISensorRepository sensors,
    IProductCatalogFacade catalog,
    IAlertsFacade alerts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ISensorCommandService
{
    public async Task<long> HandleAsync(LinkSensorCommand command)
    {
        if (command.SensorId is null || command.ProductId is null) throw DomainException.NotFound();

        var sensor = await sensors.FindByIdAsync(command.SensorId.Value) ?? throw DomainException.NotFound();
        var product = await catalog.FindByIdAsync(command.ProductId.Value) ?? throw DomainException.NotFound();

        // R9: one sensor per product and one product per sensor
        if (sensor.ProductId is { } currentProductId)
        {
            var current = await catalog.FindByIdAsync(currentProductId);
            throw DomainException.Conflict("SENSOR_IN_USE", DomainException.With(("productoNombre", current?.Name ?? string.Empty)));
        }

        if (await sensors.FindByProductIdAsync(product.Id) is not null)
            throw DomainException.Conflict("PRODUCT_HAS_SENSOR");

        sensor.LinkTo(product.Id);
        await unitOfWork.CompleteAsync();

        return sensor.Id;
    }

    public async Task<long> HandleAsync(ReceiveReadingCommand command)
    {
        // The device sends no token, so we find the sensor ignoring the business filter and then work as its business.
        // (El dispositivo no envía token: buscamos el sensor sin filtro y trabajamos como su negocio.)
        var sensor = await sensors.FindByIdIgnoringTenantAsync(command.SensorId)
            ?? throw DomainException.NotFound("SENSOR_NOT_FOUND");
        currentUser.UseBusiness(sensor.BusinessId);

        if (command.Weight is null or < 0) throw DomainException.BadRequest("INVALID_WEIGHT");

        var readAt = command.ReadAt ?? Clock.UtcNow;
        sensor.RegisterReading(command.Weight.Value, readAt);

        var reading = new SensorReading(sensor.Id, command.Weight.Value, readAt);
        await sensors.AddReadingAsync(reading);

        // R20: a linked sensor may create or resolve a discrepancy alert
        if (sensor.ProductId is { } productId)
            await alerts.OnSensorReadingAsync(productId, command.Weight.Value);

        await unitOfWork.CompleteAsync();
        return reading.Id;
    }
}

public class SensorQueryService(ISensorRepository sensors, IProductCatalogFacade catalog) : ISensorQueryService
{
    public async Task<IReadOnlyList<SensorResource>> ListAsync()
    {
        var list = await sensors.ListAsync();
        var productsById = (await catalog.ListAsync()).ToDictionary(product => product.Id);

        return list
            .Select(sensor => ToResource(sensor, sensor.ProductId is { } id ? productsById.GetValueOrDefault(id) : null))
            .ToList();
    }

    public async Task<SensorResource?> HandleAsync(GetSensorByIdQuery query)
    {
        var sensor = await sensors.FindByIdAsync(query.SensorId);
        if (sensor is null) return null;

        var product = sensor.ProductId is { } id ? await catalog.FindByIdAsync(id) : null;
        return ToResource(sensor, product);
    }

    public async Task<SensorStatusResource?> GetStatusAsync(long sensorId)
    {
        var sensor = await sensors.FindByIdAsync(sensorId);
        return sensor is null
            ? null
            : new SensorStatusResource(sensor.Status(Clock.UtcNow), sensor.LastReadingAt, sensor.WeightKg);
    }

    private static SensorResource ToResource(Sensor sensor, ProductSnapshot? product) => new(
        sensor.Id, sensor.Code, sensor.ProductId, sensor.WeightKg, sensor.LastReadingAt, sensor.ReferenceWeightKg,
        sensor.Status(Clock.UtcNow), product?.Name,
        product is null ? null : InventoryRules.ToUnits(sensor.WeightKg, product.UnitWeightKg));
}

/// <summary>What the other contexts can ask the IoT context (see IIoTDeviceFacade).</summary>
public class IoTDeviceFacade(ISensorRepository sensors) : IIoTDeviceFacade
{
    public async Task<SensorSnapshot?> FindByIdAsync(long sensorId)
    {
        var sensor = await sensors.FindByIdAsync(sensorId);
        return sensor is null ? null : ToSnapshot(sensor);
    }

    public async Task<SensorSnapshot?> FindByProductIdAsync(long productId)
    {
        var sensor = await sensors.FindByProductIdAsync(productId);
        return sensor is null ? null : ToSnapshot(sensor);
    }

    public async Task<IReadOnlyList<SensorSnapshot>> ListAsync() =>
        (await sensors.ListAsync()).Select(ToSnapshot).ToList();

    public async Task<IReadOnlyList<ReadingSnapshot>> GetLatestReadingsAsync(long sensorId, int take) =>
        (await sensors.GetLatestReadingsAsync(sensorId, take))
        .Select(reading => new ReadingSnapshot(reading.Id, reading.SensorId, reading.WeightKg, reading.ReadAt))
        .ToList();

    private static SensorSnapshot ToSnapshot(Sensor sensor) => new(
        sensor.Id, sensor.Code, sensor.ProductId, sensor.WeightKg, sensor.LastReadingAt, sensor.Status(Clock.UtcNow));
}