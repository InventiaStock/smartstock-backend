namespace SmartStock.Platform.API.InventoryMonitoring.Procurement;

public static class ProcurementFeature
{
    public static IServiceCollection AddProcurementFeature(
        this IServiceCollection services)
    {
        services.AddScoped<ISupplierRepository, SupplierRepository>();

        services.AddScoped<IPurchaseRepository, PurchaseRepository>();

        services.AddScoped<ISupplierService, SupplierService>();

        services.AddScoped<IPurchaseCommandService, PurchaseCommandService>();

        services.AddScoped<IPurchaseQueryService, PurchaseQueryService>();

        services.AddScoped<IProcurementFacade, ProcurementFacade>();

        services.AddScoped<IDemoSeeder, ProcurementDemoSeeder>();

        return services;
    }
}