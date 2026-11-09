namespace SmartStock.Platform.API.ProductCatalog;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).IsRequired().HasMaxLength(120);
        builder.Property(product => product.Sku).IsRequired().HasMaxLength(60);
        builder.Property(product => product.Category).IsRequired().HasMaxLength(60);
        builder.Property(product => product.UnitWeightKg).HasPrecision(10, 3);
        builder.Property(product => product.SalePrice).HasPrecision(10, 2);
        builder.Property(product => product.PurchaseCost).HasPrecision(10, 2);
        builder.Property(product => product.UsualSupplier).IsRequired().HasMaxLength(120);
        builder.HasIndex(product => new { product.BusinessId, product.Name });
    }
}

public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository { }

/// <summary>The 5 demo products of the Fake API (db.json). Needs an empty table so the ids are 1 to 5.</summary>
public class ProductDemoSeeder : IDemoSeeder
{
    public int Order => 20;

    public async Task SeedAsync(AppDbContext db, DemoSeedContext context)
    {
        var businessId = context.MinimarketBusinessId;
        if (businessId == 0 || await db.Set<Product>().IgnoreQueryFilters().AnyAsync(p => p.BusinessId == businessId)) return;

        const string supplier = "Fresh Foods Co.";
        (string Name, string Sku, string Category, decimal Weight, decimal? Price, decimal Cost, int Stock, int Threshold, int Capacity)[] rows =
        [
            ("Rice 1 kg", "RICE-1KG", "Grocery", 1.0m, 8.2m, 6.5m, 2, 5, 50),
            ("Cooking Oil 1 L", "OIL-1L", "Grocery", 0.92m, 10.5m, 8.5m, 4, 6, 40),
            ("Milk 1 L", "MILK-1L", "Dairy", 1.03m, 5.5m, 4.2m, 14, 5, 60),
            ("Sugar 1 kg", "SUGAR-1KG", "Grocery", 1.0m, 4.8m, 3.7m, 20, 6, 50),
            ("Flour 1 kg", "FLOUR-1KG", "Grocery", 1.0m, null, 2.9m, 7, 5, 50),
        ];

        foreach (var row in rows)
        {
            var product = new Product(row.Name, row.Sku, row.Category, row.Weight, row.Price, row.Cost, supplier, row.Threshold, row.Capacity)
            {
                BusinessId = businessId,
            };
            product.SetStockForSeed(row.Stock);
            db.Set<Product>().Add(product);
        }

        await db.SaveChangesAsync();
    }
}
