namespace SmartStock.Platform.API.AlertsRestocking;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");
        builder.HasKey(alert => alert.Id);
        builder.Property(alert => alert.Type).IsRequired().HasMaxLength(20);
        builder.Property(alert => alert.Source).IsRequired().HasMaxLength(15);
        builder.Property(alert => alert.Status).IsRequired().HasMaxLength(15);
        builder.Ignore(alert => alert.IsActive);
        builder.HasIndex(alert => new { alert.BusinessId, alert.ProductId, alert.Type, alert.Status });
    }
}

public class RestockingNeedConfiguration : IEntityTypeConfiguration<RestockingNeed>
{
    public void Configure(EntityTypeBuilder<RestockingNeed> builder)
    {
        builder.ToTable("restocking_needs");
        builder.HasKey(need => need.Id);
        builder.Property(need => need.Status).IsRequired().HasMaxLength(15);
        builder.Property(need => need.PurchaseCode).HasMaxLength(20);
        builder.HasOne(need => need.Alert).WithMany().HasForeignKey(need => need.AlertId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationChannelConfiguration : IEntityTypeConfiguration<NotificationChannel>
{
    public void Configure(EntityTypeBuilder<NotificationChannel> builder)
    {
        builder.ToTable("notification_channels");
        builder.HasKey(channel => channel.Id);
        builder.Property(channel => channel.Channel).IsRequired().HasMaxLength(15);
        builder.Property(channel => channel.Destination).IsRequired().HasMaxLength(160);
    }
}

public class SentNotificationConfiguration : IEntityTypeConfiguration<SentNotification>
{
    public void Configure(EntityTypeBuilder<SentNotification> builder)
    {
        builder.ToTable("sent_notifications");
        builder.HasKey(sent => sent.Id);
        builder.Property(sent => sent.Channel).IsRequired().HasMaxLength(15);
        builder.Property(sent => sent.Destination).IsRequired().HasMaxLength(160);
        builder.Property(sent => sent.Error).HasMaxLength(500);
        builder.HasOne(sent => sent.Alert).WithMany().HasForeignKey(sent => sent.AlertId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AlertRepository(AppDbContext context) : BaseRepository<Alert>(context), IAlertRepository
{
    public async Task<Alert?> FindActiveAsync(long productId, string type)
    {
        // Alerts created earlier in the same request are not in the database yet: look in memory first.
        // (Las alertas creadas en la misma solicitud aún no están en la BD: se busca primero en memoria.)
        var local = Context.Set<Alert>().Local
            .FirstOrDefault(alert => alert.ProductId == productId && alert.Type == type && alert.IsActive);
        if (local is not null) return local;

        return await Context.Set<Alert>().FirstOrDefaultAsync(
            alert => alert.ProductId == productId && alert.Type == type && alert.Status == AlertStatuses.Active);
    }

    public Task<Alert?> FindActiveByProductAsync(long productId) =>
        Context.Set<Alert>()
            .Where(alert => alert.ProductId == productId && alert.Status == AlertStatuses.Active)
            .OrderBy(alert => alert.Id)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<Alert>> ListVisibleAsync()
    {
        var since = Clock.UtcNow.AddHours(-24);
        return await Context.Set<Alert>()
            .Where(alert => alert.Status == AlertStatuses.Active || (alert.ResolvedAt != null && alert.ResolvedAt >= since))
            .OrderByDescending(alert => alert.Id)
            .ToListAsync();
    }

    public Task<int> CountActiveAsync(string type) =>
        Context.Set<Alert>().CountAsync(alert => alert.Type == type && alert.Status == AlertStatuses.Active);
}

public class RestockingNeedRepository(AppDbContext context)
    : BaseRepository<RestockingNeed>(context), IRestockingNeedRepository
{
    public async Task<IReadOnlyList<RestockingNeed>> ListByAlertIdsAsync(IEnumerable<long> alertIds)
    {
        var ids = alertIds.ToList();
        return await Context.Set<RestockingNeed>().Where(need => ids.Contains(need.AlertId)).ToListAsync();
    }
}

public class NotificationChannelRepository(AppDbContext context)
    : BaseRepository<NotificationChannel>(context), INotificationChannelRepository { }

public class SentNotificationRepository(AppDbContext context)
    : BaseRepository<SentNotification>(context), ISentNotificationRepository { }

/// <summary>The 3 alerts, 2 restocking needs and 2 channels of the Fake API (db.json).</summary>
public class AlertDemoSeeder : IDemoSeeder
{
    public int Order => 70;

    public async Task SeedAsync(AppDbContext db, DemoSeedContext context)
    {
        var businessId = context.MinimarketBusinessId;
        if (businessId == 0 || await db.Set<Alert>().IgnoreQueryFilters().AnyAsync(a => a.BusinessId == businessId)) return;

        var lowRice = Alert.CreateLowStock(1, 2, "sensor");
        var lowOil = Alert.CreateLowStock(2, 4, "registered");
        var discrepancy = Alert.CreateDiscrepancy(3, 14, 12, -14.3);

        foreach (var alert in new[] { lowRice, lowOil, discrepancy })
        {
            alert.BusinessId = businessId;
            db.Set<Alert>().Add(alert);
        }

        db.Set<RestockingNeed>().Add(new RestockingNeed(lowRice) { BusinessId = businessId });
        db.Set<RestockingNeed>().Add(new RestockingNeed(lowOil) { BusinessId = businessId });
        db.Set<NotificationChannel>().Add(
            new NotificationChannel(NotificationChannel.Email, "store.owner@email.com", true) { BusinessId = businessId });
        db.Set<NotificationChannel>().Add(
            new NotificationChannel(NotificationChannel.WhatsApp, "+51 987 654 321", true) { BusinessId = businessId });

        await db.SaveChangesAsync();
    }
}
