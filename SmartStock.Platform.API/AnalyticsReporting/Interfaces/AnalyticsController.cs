namespace SmartStock.Platform.API.AnalyticsReporting;

// JSON names come from WireNamingPolicy: TodaySales -> ventasHoy, StockSummary -> resumenStock, etc.
public record DayTotalResource(decimal Total, int Count);

// CreatedAt only sorts the list; it is not sent to the frontend. (CreatedAt solo ordena; no se envía.)
public record RecentActivityResource(
    string Type, string Id, decimal Total, DateOnly Date,
    [property: System.Text.Json.Serialization.JsonIgnore] DateTime CreatedAt);

public record StockSummaryItemResource(long ProductId, string Name, int Units, string Level, string Sensor);

public record DashboardResource(
    DayTotalResource TodaySales,
    DayTotalResource TodayPurchases,
    int LowStockAlerts,
    IReadOnlyList<RecentActivityResource> RecentActivity,
    IReadOnlyList<StockSummaryItemResource> StockSummary);

public record ReportResource(
    DateOnly? From,
    DateOnly? To,
    decimal SalesTotal,
    decimal PurchasesTotal,
    IReadOnlyList<StockMovementResource> Movements,
    IReadOnlyList<string> PendingPurchases);

/// <summary>US15 (dashboard) and US25 (reports).</summary>
[ApiController]
[Tags("Analytics & Reporting")]
public class AnalyticsController(IAnalyticsQueryService queries) : ControllerBase
{
    [HttpGet("api/dashboard")]
    [EndpointSummary("Gets the summary of the day (US15)")]
    [EndpointDescription("Sales and purchases of today, active low stock alerts, the 5 most recent operations and the stock level of every product.")]
    [ProducesResponseType(typeof(DashboardResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard() => Ok(await queries.HandleAsync(new GetDashboardQuery()));

    [HttpGet("api/reportes")]
    [EndpointSummary("Gets the report of a period (US25)")]
    [EndpointDescription("Filters desde/hasta (yyyy-MM-dd). Totals of sales and purchases, stock movements and the pending purchases. A PENDING purchase does not create an IN movement.")]
    [ProducesResponseType(typeof(ReportResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReport(
        [FromQuery(Name = "desde")] DateOnly? from, [FromQuery(Name = "hasta")] DateOnly? to) =>
        Ok(await queries.HandleAsync(new GetReportQuery(from, to)));
}