namespace SmartStock.Platform.API.AnalyticsReporting;

public record GetDashboardQuery;

public record GetReportQuery(DateOnly? From, DateOnly? To);

public interface IAnalyticsQueryService
{
    /// <summary>US15: summary of the day.</summary>
    Task<DashboardResource> HandleAsync(GetDashboardQuery query);

    /// <summary>US25: totals and stock movements of a period.</summary>
    Task<ReportResource> HandleAsync(GetReportQuery query);
}

/// <summary>
/// Analytics only READS: it asks the other contexts through their facades and never changes the inventory,
/// and it keeps no data of its own. (Analytics solo lee, no modifica ni guarda datos propios.)
/// </summary>
public class AnalyticsQueryService(
    ISalesFacade sales,
    IProcurementFacade procurement,
    IStockMovementFacade movements,
    IAlertsFacade alerts,
    IProductCatalogFacade catalog,
    IIoTDeviceFacade iot) : IAnalyticsQueryService
{
    public async Task<DashboardResource> HandleAsync(GetDashboardQuery query)
    {
        var today = Clock.Today;

        var todaySales = (await sales.ListAsync(today, today))
            .Where(sale => sale.Status == SaleStatuses.Completed).ToList();
        var todayPurchases = (await procurement.ListAsync(today, today))
            .Where(purchase => purchase.Status != PurchaseStatuses.Cancelled).ToList();

        var recent = todaySales.Select(s => new RecentActivityResource("SALE", s.Code, s.Total, s.Date, s.CreatedAt))
            .Concat(todayPurchases.Select(p => new RecentActivityResource("PURCHASE", p.Code, p.Total, p.Date, p.CreatedAt)))
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .ToList();

        var sensorsByProduct = (await iot.ListAsync())
            .Where(sensor => sensor.ProductId is not null)
            .ToDictionary(sensor => sensor.ProductId!.Value);

        var stockSummary = (await catalog.ListAsync())
            .Select(product => ProductViewFactory.Create(product, sensorsByProduct.GetValueOrDefault(product.Id)))
            .Select(view => new StockSummaryItemResource(view.Id, view.Name, view.ReferenceStock, view.StockLevel, view.SensorStatus))
            .ToList();

        return new DashboardResource(
            new DayTotalResource(InventoryRules.Money(todaySales.Sum(s => s.Total)), todaySales.Count),
            new DayTotalResource(InventoryRules.Money(todayPurchases.Sum(p => p.Total)), todayPurchases.Count),
            await alerts.CountActiveLowStockAsync(),
            recent,
            stockSummary);
    }

    public async Task<ReportResource> HandleAsync(GetReportQuery query)
    {
        var periodSales = (await sales.ListAsync(query.From, query.To))
            .Where(sale => sale.Status == SaleStatuses.Completed).ToList();
        var periodPurchases = (await procurement.ListAsync(query.From, query.To))
            .Where(purchase => purchase.Status != PurchaseStatuses.Cancelled).ToList();
        var periodMovements = await movements.ListAsync(query.From, query.To);

        return new ReportResource(
            query.From,
            query.To,
            InventoryRules.Money(periodSales.Sum(s => s.Total)),
            InventoryRules.Money(periodPurchases.Sum(p => p.Total)),
            periodMovements.Select(StockMovementResource.From).ToList(),
            periodPurchases.Where(p => p.Status == PurchaseStatuses.Pending).Select(p => p.Code).ToList());
    }
}