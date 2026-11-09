namespace SmartStock.Platform.API.InventoryMonitoring.Sales;

public static class SalesFeature
{
    public static IServiceCollection AddSalesFeature(this IServiceCollection services)
    {
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleCommandService, SaleCommandService>();
        services.AddScoped<ISaleQueryService, SaleQueryService>();
        services.AddScoped<ISalesFacade, SalesFacade>();   // replaces the stub (reemplaza el stub)
        services.AddScoped<IDemoSeeder, SaleDemoSeeder>();
        return services;
    }
}
