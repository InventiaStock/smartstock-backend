namespace SmartStock.Platform.API.IoTDevice;

public record SensorResource(
    long Id, string Code, long? ProductId, decimal WeightKg, DateTime? LastReadingAt, decimal? ReferenceWeightKg,
    string Status, string? ProductName, int? Units);

public record SensorStatusResource(string Status, DateTime? LastReadingAt, decimal WeightKg);

public record LinkSensorRequest(long? SensorId, long? ProductId);

public record ReadingRequest(decimal? Weight, DateTime? ReadAt);

/// <summary>US04, US06, TS01 and TS05 - sensors.</summary>
[ApiController]
[Route("api/sensores")]
[Tags("IoT Device")]
public class SensorsController(
    ISensorCommandService commands,
    ISensorQueryService queries,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists the sensors with their connection state")]
    [EndpointDescription("Status: online (last reading at most 5 minutes old, R11), disconnected, or available (no product).")]
    [ProducesResponseType(typeof(IReadOnlyList<SensorResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await queries.ListAsync());

    [HttpGet("{id:long}")]
    [EndpointSummary("Gets one sensor")]
    [ProducesResponseType(typeof(SensorResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id) =>
        Ok(await queries.HandleAsync(new GetSensorByIdQuery(id)) ?? throw DomainException.NotFound());

    [HttpPost("vincular")]
    [EndpointSummary("Links a sensor to a product (US04)")]
    [EndpointDescription("409 SENSOR_IN_USE if the sensor already has a product; 409 PRODUCT_HAS_SENSOR if the product already has a sensor (R9).")]
    [ProducesResponseType(typeof(SensorResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Link([FromBody] LinkSensorRequest request)
    {
        var sensorId = await commands.HandleAsync(new LinkSensorCommand(request.SensorId, request.ProductId));
        return Ok(await queries.HandleAsync(new GetSensorByIdQuery(sensorId)));
    }

    [HttpGet("{id:long}/estado")]
    [EndpointSummary("Gets the connection state of a sensor (TS05)")]
    [ProducesResponseType(typeof(SensorStatusResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(long id) =>
        Ok(await queries.GetStatusAsync(id) ?? throw DomainException.NotFound());

    [HttpPost("{id:long}/lecturas")]
    [AllowAnonymous]
    [EndpointSummary("Receives a weight reading from the microcontroller (TS01)")]
    [EndpointDescription("Public endpoint for the devices. If Devices:ApiKey is configured, the header X-Device-Key is required.")]
    [ProducesResponseType(typeof(CreatedIdResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveReading(long id, [FromBody] ReadingRequest request)
    {
        var expectedKey = configuration["Devices:ApiKey"];
        if (!string.IsNullOrEmpty(expectedKey) && Request.Headers["X-Device-Key"] != expectedKey)
            throw new DomainException("UNAUTHORIZED", 401);

        var readingId = await commands.HandleAsync(new ReceiveReadingCommand(id, request.Weight, request.ReadAt));
        return StatusCode(StatusCodes.Status201Created, new CreatedIdResource(readingId));
    }
}