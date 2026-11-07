namespace SmartStock.Platform.API.ProductCatalog;

public static class ProductCatalogModule
{
    public static IServiceCollection AddProductCatalogModule(
        this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<IProductCommandService, ProductCommandService>();

        services.AddScoped<IProductQueryService, ProductQueryService>();

        services.AddScoped<IProductCatalogFacade, ProductCatalogFacade>();

        services.AddScoped<IDemoSeeder, ProductDemoSeeder>();

        return services;
    }
}