namespace SmartStock.Platform.API.IoTDevice;

public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("sensors");
        builder.HasKey(sensor => sensor.Id);
        builder.Property(sensor => sensor.Code).IsRequired().HasMaxLength(30);
        builder.Property(sensor => sensor.WeightKg).HasPrecision(10, 3);
        builder.Property(sensor => sensor.ReferenceWeightKg).HasPrecision(10, 3);
        builder.HasIndex(sensor => new { sensor.BusinessId, sensor.Code }).IsUnique();
        builder.HasIndex(sensor => sensor.ProductId).IsUnique();   // R9: one sensor per product (NULLs are allowed many times in MySQL)
    }
}

public class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
{
    public void Configure(EntityTypeBuilder<SensorReading> builder)
    {
        builder.ToTable("sensor_readings");
        builder.HasKey(reading => reading.Id);
        builder.Property(reading => reading.WeightKg).HasPrecision(10, 3);
        builder.HasOne<Sensor>().WithMany().HasForeignKey(reading => reading.SensorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(reading => new { reading.SensorId, reading.ReadAt });
    }
}

public class SensorRepository(AppDbContext context) : BaseRepository<Sensor>(context), ISensorRepository
{
    public Task<Sensor?> FindByIdIgnoringTenantAsync(long sensorId) =>
        Context.Set<Sensor>().IgnoreQueryFilters().FirstOrDefaultAsync(sensor => sensor.Id == sensorId);

    public Task<Sensor?> FindByProductIdAsync(long productId) =>
        Context.Set<Sensor>().FirstOrDefaultAsync(sensor => sensor.ProductId == productId);

    public async Task AddReadingAsync(SensorReading reading) => await Context.Set<SensorReading>().AddAsync(reading);

    public async Task<IReadOnlyList<SensorReading>> GetLatestReadingsAsync(long sensorId, int take) =>
        await Context.Set<SensorReading>()
            .Where(reading => reading.SensorId == sensorId)
            .OrderByDescending(reading => reading.ReadAt)
            .Take(take)
            .ToListAsync();
}

/// <summary>The 4 demo sensors and 7 readings of the Fake API. Needs the products 1 to 3 to exist.</summary>
public class SensorDemoSeeder : IDemoSeeder
{
    public int Order => 30;

    public async Task SeedAsync(AppDbContext db, DemoSeedContext context)
    {
        var businessId = context.MinimarketBusinessId;
        if (businessId == 0 || await db.Set<Sensor>().IgnoreQueryFilters().AnyAsync(s => s.BusinessId == businessId)) return;

        var now = Clock.UtcNow;
        (string Code, long? ProductId, decimal Weight, int? MinutesAgo)[] rows =
        [
            ("SN-0001", 1, 2.0m, 2),
            ("SN-0002", 2, 3.68m, 18),
            ("SN-0003", 3, 12.36m, 1),
            ("SN-0004", null, 20.0m, null),
        ];

        foreach (var row in rows)
        {
            var sensor = new Sensor(row.Code, row.Weight) { BusinessId = businessId };
            sensor.SetStateForSeed(row.ProductId, row.MinutesAgo is { } minutes ? now.AddMinutes(-minutes) : null);
            db.Set<Sensor>().Add(sensor);
        }

        await db.SaveChangesAsync();

        (long SensorId, decimal Weight, int MinutesAgo)[] readings =
        [
            (1, 2.0m, 2), (1, 2.0m, 32), (1, 4.0m, 62),
            (2, 3.68m, 18), (2, 3.68m, 48),
            (3, 12.36m, 1), (3, 14.42m, 31),
        ];

        foreach (var reading in readings)
            db.Set<SensorReading>().Add(new SensorReading(reading.SensorId, reading.Weight, now.AddMinutes(-reading.MinutesAgo)));

        await db.SaveChangesAsync();
    }
}
