using Microsoft.Extensions.DependencyInjection.Extensions;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>Context that the demo seeders share. (Contexto compartido por los seeders de datos demo.)</summary>
public class DemoSeedContext
{
    public long MinimarketBusinessId { get; set; }
    public long BodegaBusinessId { get; set; }
}

/// <summary>
/// Each context fills its own demo data (same data as the Fake API db.json), in this order:
/// 10 IAM, 20 Catalog, 30 IoT, 40 Procurement, 50 Sales, 60 Stock, 70 Alerts.
/// (Cada contexto siembra sus datos demo, los mismos del db.json de la Fake API.)
/// </summary>
public interface IDemoSeeder
{
    int Order { get; }

    Task SeedAsync(AppDbContext db, DemoSeedContext context);
}

public static class SharedKernelExtensions
{
    public static IServiceCollection AddSharedKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Stubs: they are replaced when each owner registers the real class (the last registration wins).
        // (Stubs: se reemplazan cuando cada responsable registra su clase real.)
        services.TryAddScoped<IProductCatalogFacade, NotReadyProductCatalogFacade>();
        services.TryAddScoped<IIoTDeviceFacade, NotReadyIoTDeviceFacade>();
        services.TryAddScoped<IStockMovementFacade, NotReadyStockMovementFacade>();
        services.TryAddScoped<ISalesFacade, NotReadySalesFacade>();
        services.TryAddScoped<IProcurementFacade, NotReadyProcurementFacade>();
        services.TryAddScoped<IAlertsFacade, NotReadyAlertsFacade>();
        services.TryAddScoped<IEmailSender, ConsoleEmailSender>();
        services.TryAddScoped<IWhatsAppSender, NoopWhatsAppSender>();

        return services;
    }

    /// <summary>
    /// Creates the tables and, in development, the demo data.
    /// Database:UseMigrations = false -> EnsureCreated (while the model changes every day).
    /// Database:UseMigrations = true  -> Migrate (final version, after the InitialCreate migration).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var configuration = app.Configuration;
        if (!configuration.GetValue("Database:AutoInitialize", true)) return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (configuration.GetValue("Database:UseMigrations", false))
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        if (!configuration.GetValue("Database:SeedDemoData", false)) return;

        var context = new DemoSeedContext();
        foreach (var seeder in scope.ServiceProvider.GetServices<IDemoSeeder>().OrderBy(seeder => seeder.Order))
            await seeder.SeedAsync(db, context);
    }
}
