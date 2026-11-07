using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartStock.Platform.API.AlertsRestocking;

[ApiController]
[Tags("Alerts & Restocking")]
public class AlertsController(
    IAlertQueryService alerts,
    INotificationService notifications) : ControllerBase
{
    [HttpGet("api/alertas")]
    [EndpointSummary(
        "Lists the active alerts and the ones resolved in the last 24 hours")]
    [EndpointDescription(
        "Type LOW_STOCK (R19) or DISCREPANCY (R20). "
        + "needId identifies the restocking need used to register a purchase from the alert (R21).")]
    [ProducesResponseType(
        typeof(IReadOnlyList<AlertResource>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts()
    {
        return Ok(await alerts.ListAsync());
    }

    [HttpGet("api/canales-notificacion")]
    [EndpointSummary(
        "Lists the notification channels (email and WhatsApp)")]
    [ProducesResponseType(
        typeof(IReadOnlyList<ChannelResource>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChannels()
    {
        return Ok(await notifications.ListChannelsAsync());
    }
}