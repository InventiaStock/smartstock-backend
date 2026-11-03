namespace SmartStock.Platform.API.InventoryMonitoring.Stock;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Type).IsRequired().HasMaxLength(10);
        builder.Property(movement => movement.Origin).IsRequired().HasMaxLength(15);
        builder.Property(movement => movement.OriginCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(movement => new { movement.BusinessId, movement.Date });
        builder.HasIndex(movement => new { movement.Origin, movement.OriginCode });
    }
}

public class StockMovementRepository(AppDbContext context)
    : BaseRepository<StockMovement>(context), IStockMovementRepository
{
    public async Task<IReadOnlyList<StockMovement>> ListByDateRangeAsync(DateOnly? from, DateOnly? to) =>
        await Context.Set<StockMovement>()
            .Where(m => (from == null || m.Date >= from) && (to == null || m.Date <= to))
            .OrderByDescending(m => m.Id)
            .ToListAsync();

    public async Task<IReadOnlyList<StockMovement>> ListByOriginAsync(string origin, string originCode) =>
        await Context.Set<StockMovement>()
            .Where(m => m.Origin == origin && m.OriginCode == originCode)
            .OrderBy(m => m.Id)
            .ToListAsync();
}

/// <summary>The 3 movements of the sale R-00041 (the products already have their stock after that sale).</summary>
public class StockMovementDemoSeeder : IDemoSeeder
{
    public int Order => 60;

    public async Task SeedAsync(AppDbContext db, DemoSeedContext context)
    {
        var businessId = context.MinimarketBusinessId;
        if (businessId == 0 || await db.Set<StockMovement>().IgnoreQueryFilters().AnyAsync(m => m.BusinessId == businessId)) return;

        var today = Clock.Today;
        (long ProductId, int Quantity)[] rows = [(3, 3), (1, 2), (4, 4)];

        foreach (var row in rows)
        {
            db.Set<StockMovement>().Add(new StockMovement(
                row.ProductId, MovementTypes.Out, MovementOrigins.Sale, "R-00041", row.Quantity, today)
            {
                BusinessId = businessId,
            });
        }

        await db.SaveChangesAsync();
    }
}
