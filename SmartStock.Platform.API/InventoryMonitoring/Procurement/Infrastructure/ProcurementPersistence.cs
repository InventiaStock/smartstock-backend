namespace SmartStock.Platform.API.InventoryMonitoring.Procurement;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(supplier => supplier.Id);
        builder.Property(supplier => supplier.Name).IsRequired().HasMaxLength(120);
        builder.Property(supplier => supplier.Phone).IsRequired().HasMaxLength(30);
        builder.Property(supplier => supplier.Email).IsRequired().HasMaxLength(160);
        builder.HasIndex(supplier => new { supplier.BusinessId, supplier.Name, supplier.Phone });
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases");
        builder.HasKey(purchase => purchase.Id);
        builder.Ignore(purchase => purchase.Code);   // calculated: "PO-" + Number (calculado)
        builder.Property(purchase => purchase.Status).IsRequired().HasMaxLength(15);
        builder.Property(purchase => purchase.Total).HasPrecision(12, 2);
        builder.HasIndex(purchase => new { purchase.BusinessId, purchase.Number }).IsUnique();
        builder.HasIndex(purchase => new { purchase.BusinessId, purchase.Date });
        builder.HasOne<Supplier>().WithMany().HasForeignKey(purchase => purchase.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(purchase => purchase.Items).WithOne().HasForeignKey(item => item.PurchaseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.UnitCost).HasPrecision(10, 2);
    }
}

public class SupplierRepository(AppDbContext context) : BaseRepository<Supplier>(context), ISupplierRepository { }

public class PurchaseRepository(AppDbContext context) : BaseRepository<Purchase>(context), IPurchaseRepository
{
    public async Task<int> NextNumberAsync() =>
        (await Context.Set<Purchase>().MaxAsync(purchase => (int?)purchase.Number) ?? 0) + 1;

    public Task<Purchase?> FindByCodeAsync(string code) =>
        CodeFormat.TryParse(Purchase.CodePrefix, code, out var number)
            ? Context.Set<Purchase>().Include(purchase => purchase.Items).FirstOrDefaultAsync(purchase => purchase.Number == number)
            : Task.FromResult<Purchase?>(null);

    public async Task<IReadOnlyList<Purchase>> ListByDateRangeAsync(DateOnly? from, DateOnly? to) =>
        await Context.Set<Purchase>()
            .Include(purchase => purchase.Items)
            .Where(purchase => (from == null || purchase.Date >= from) && (to == null || purchase.Date <= to))
            .OrderByDescending(purchase => purchase.Number)
            .ToListAsync();
}

/// <summary>The supplier "Fresh Foods Co." and the pending purchase PO-00031 of the Fake API.</summary>
public class ProcurementDemoSeeder : IDemoSeeder
{
    public int Order => 40;

    public async Task SeedAsync(AppDbContext db, DemoSeedContext context)
    {
        var businessId = context.MinimarketBusinessId;
        if (businessId == 0 || await db.Set<Supplier>().IgnoreQueryFilters().AnyAsync(s => s.BusinessId == businessId)) return;

        var supplier = new Supplier("Fresh Foods Co.", "+51 999 123 456", "orders@freshfoods.pe") { BusinessId = businessId };
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync();

        var purchase = new Purchase(31, supplier.Id, Clock.Today, null,
        [
            new PurchaseItem(1, 10, 6.5m),
            new PurchaseItem(2, 8, 8.5m),
        ])
        {
            BusinessId = businessId,
        };

        db.Set<Purchase>().Add(purchase);
        await db.SaveChangesAsync();
    }
}
