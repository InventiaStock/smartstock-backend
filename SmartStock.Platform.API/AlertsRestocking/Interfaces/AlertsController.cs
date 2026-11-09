namespace SmartStock.Platform.API.AlertsRestocking;

public record ChannelChangeRequest(long Id, bool Active);

public record NotifyRequest(long? ProductId);

[ApiController]
[Tags("Alerts & Restocking")]
public class AlertsController(
    IAlertQueryService alerts,
    INotificationService notifications) : ControllerBase
{
    [HttpGet("api/alertas")]
    [EndpointSummary("Lists the active alerts and the ones resolved in the last 24 hours")]
    [EndpointDescription("Type LOW_STOCK (R19) or DISCREPANCY (R20). needId identifies the restocking need used to register a purchase from the alert (R21).")]
    [ProducesResponseType(
        typeof(IReadOnlyList<AlertResource>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts()
    {
        return Ok(await alerts.ListAsync());
    }

    [HttpGet("api/canales-notificacion")]
    [EndpointSummary("Lists the notification channels (email and WhatsApp)")]
    [ProducesResponseType(
        typeof(IReadOnlyList<ChannelResource>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChannels()
    {
        return Ok(await notifications.ListChannelsAsync());
    }

    [HttpPut("api/canales-notificacion")]
    [EndpointSummary("Activates or deactivates the notification channels")]
    [EndpointDescription("422 AT_LEAST_ONE_CHANNEL if the change would leave every channel inactive.")]
    [ProducesResponseType(
        typeof(IReadOnlyList<ChannelResource>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateChannels(
        [FromBody] IReadOnlyList<ChannelChangeRequest>? changes)
    {
        var command = new UpdateChannelsCommand(
            (changes ?? [])
            .Select(change => new ChannelChange(
                change.Id, change.Active))
            .ToList());

        return Ok(await notifications.HandleAsync(command));
    }

    [HttpPost("api/notificaciones/stock-bajo")]
    [EndpointSummary("Sends the notification of the active alert of a product (TS03)")]
    [EndpointDescription("Sends the message through every active channel. 404 ALERT_NOT_FOUND if the product has no active alert.")]
    [ProducesResponseType(
        typeof(NotificationSentResource),
        StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyLowStock(
        [FromBody] NotifyRequest request)
    {
        var result = await notifications.HandleAsync(
            new NotifyLowStockCommand(request.ProductId));

        return StatusCode(StatusCodes.Status202Accepted, result);
    }
}