namespace SmartStock.Platform.API.InventoryMonitoring.Stock;

public static class StockFeature
{
    public static IServiceCollection AddStockFeature(this IServiceCollection services)
    {
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IStockMovementFacade, StockMovementFacade>();   // replaces the stub (reemplaza el stub)
        services.AddScoped<IStockQueryService, StockQueryService>();
        services.AddScoped<IDemoSeeder, StockMovementDemoSeeder>();
        return services;
    }
}
