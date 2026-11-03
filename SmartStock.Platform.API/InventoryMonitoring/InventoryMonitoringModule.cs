using SmartStock.Platform.API.InventoryMonitoring.Procurement;
using SmartStock.Platform.API.InventoryMonitoring.Sales;
using SmartStock.Platform.API.InventoryMonitoring.Stock;

namespace SmartStock.Platform.API.InventoryMonitoring;

/// <summary>
/// Inventory Monitoring has three features, each one with its own owner:
/// Stock (Geraldine), Sales (Lorena) and Procurement = suppliers + purchases (Candy).
/// </summary>
public static class InventoryMonitoringModule
{
    public static IServiceCollection AddInventoryMonitoringModule(this IServiceCollection services)
    {
        services.AddStockFeature();
        services.AddSalesFeature();
        services.AddProcurementFeature();
        return services;
    }
}
