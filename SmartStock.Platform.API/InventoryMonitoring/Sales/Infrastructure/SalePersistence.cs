using SmartStock.Platform.API.ProductCatalog;
namespace SmartStock.Platform.API.InventoryMonitoring.Sales;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");
        builder.HasKey(sale => sale.Id);
        builder.Ignore(sale => sale.Code);   // calculated: "R-" + Number (calculado)
        builder.Property(sale => sale.Status).IsRequired().HasMaxLength(15);
        builder.Property(sale => sale.Total).HasPrecision(12, 2);
        builder.HasIndex(sale => new { sale.BusinessId, sale.Number }).IsUnique();
        builder.HasIndex(sale => new { sale.BusinessId, sale.Date });
        builder.HasMany(sale => sale.Items).WithOne().HasForeignKey(item => item.SaleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.UnitPrice).HasPrecision(10, 2);
    }
}

public class SaleRepository(AppDbContext context) : BaseRepository<Sale>(context), ISaleRepository
{
    public async Task<int> NextNumberAsync() =>
        (await Context.Set<Sale>().MaxAsync(sale => (int?)sale.Number) ?? 0) + 1;

    public Task<Sale?> FindByCodeAsync(string code) =>
        CodeFormat.TryParse(Sale.CodePrefix, code, out var number)
            ? Context.Set<Sale>().Include(sale => sale.Items).FirstOrDefaultAsync(sale => sale.Number == number)
            : Task.FromResult<Sale?>(null);

    public async Task<IReadOnlyList<Sale>> ListByDateRangeAsync(DateOnly? from, DateOnly? to) =>
        await Context.Set<Sale>()
            .Include(sale => sale.Items)
            .Where(sale => (from == null || sale.Date >= from) && (to == null || sale.Date <= to))
            .OrderByDescending(sale => sale.Number)
            .ToListAsync();
}

/// <summary>The sale R-00041 of the Fake API (3 items, total 52.10).</summary>
public class SaleDemoSeeder : IDemoSeeder
{
    public int Order => 50;

    public async Task SeedAsync(AppDbContext db, DemoSeedContext context)
    {
        var businessId = context.MinimarketBusinessId;
        if (businessId == 0 || await db.Set<Sale>().IgnoreQueryFilters().AnyAsync(s => s.BusinessId == businessId)) return;

        // The demo sale needs the demo products (they arrive with the catalog). (Necesita los productos demo.)
        if (!await db.Set<Product>().IgnoreQueryFilters().AnyAsync(p => p.BusinessId == businessId)) return;
        var sale = new Sale(41, Clock.Today,
        [
            new SaleItem(3, 3, 5.5m),
            new SaleItem(1, 2, 8.2m),
            new SaleItem(4, 4, 4.8m),
        ])
        {
            BusinessId = businessId,
        };

        db.Set<Sale>().Add(sale);
        await db.SaveChangesAsync();
    }
}
