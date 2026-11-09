namespace SmartStock.Platform.API.AlertsRestocking;

public interface INotificationDispatcher
{
    Task NotifyAsync(Alert alert, string productName, int minThreshold);
}

// Rules R19–R21. The caller saves the transaction.
public class AlertsFacade(
    IAlertRepository alerts,
    IRestockingNeedRepository needs,
    IProductCatalogFacade catalog,
    IIoTDeviceFacade iot,
    INotificationDispatcher dispatcher) : IAlertsFacade
{
    public async Task OnStockChangedAsync(
        long productId,
        string movementType,
        int newRegisteredStock)
    {
        var product = await catalog.FindByIdAsync(productId);
        if (product is null) return;

        var active = await alerts.FindActiveAsync(
            productId, AlertTypes.LowStock);

        // R19: an exit leaves stock at or below the threshold.
        if (movementType == MovementTypes.Out
            && newRegisteredStock <= product.MinThreshold
            && active is null)
        {
            var sensor = await iot.FindByProductIdAsync(productId);
            var source = sensor is { Status: SensorStatuses.Online }
                ? "sensor"
                : "registered";

            var alert = Alert.CreateLowStock(
                productId, newRegisteredStock, source);

            await alerts.AddAsync(alert);
            await needs.AddAsync(new RestockingNeed(alert));
            await dispatcher.NotifyAsync(
                alert, product.Name, product.MinThreshold);
        }

        // An entry raises stock above the threshold.
        if (movementType == MovementTypes.In
            && newRegisteredStock > product.MinThreshold
            && active is not null)
        {
            await ResolveAsync(active);
        }
    }

    public async Task OnSensorReadingAsync(long productId, decimal weightKg)
    {
        var product = await catalog.FindByIdAsync(productId);
        if (product is null) return;

        var physical = InventoryRules.ToUnits(
            weightKg, product.UnitWeightKg);

        var pct = InventoryRules.RawDifferencePct(
            physical, product.RegisteredStock);

        var active = await alerts.FindActiveAsync(
            productId, AlertTypes.Discrepancy);

        // R20: the difference exceeds 10%.
        if (Math.Abs(pct) > InventoryRules.DiscrepancyThresholdPct
            && active is null)
        {
            var alert = Alert.CreateDiscrepancy(
                productId, product.RegisteredStock, physical, pct);

            await alerts.AddAsync(alert);
            await dispatcher.NotifyAsync(
                alert, product.Name, product.MinThreshold);
        }
        else if (Math.Abs(pct) <= InventoryRules.DiscrepancyThresholdPct
                 && active is not null)
        {
            active.Resolve();
        }
    }

    public async Task OnPurchaseRegisteredAsync(
        string purchaseCode,
        long? restockingNeedId)
    {
        var need = restockingNeedId is { } id
            ? await needs.FindByIdAsync(id)
            : null;

        need?.StartPurchase(purchaseCode);
    }

    public async Task OnPurchaseReceivedAsync(
        string purchaseCode,
        long? restockingNeedId)
    {
        var need = restockingNeedId is { } id
            ? await needs.FindByIdAsync(id)
            : null;

        if (need is null) return;

        var alert = await alerts.FindByIdAsync(need.AlertId);
        if (alert is { IsActive: true }) alert.Resolve();

        need.Complete();
    }

    public Task<int> CountActiveLowStockAsync() =>
        alerts.CountActiveAsync(AlertTypes.LowStock);

    public async Task<long?> GetActiveDiscrepancyAlertIdAsync()
    {
        var visible = await alerts.ListVisibleAsync();

        return visible.FirstOrDefault(
            alert => alert.Type == AlertTypes.Discrepancy
                     && alert.IsActive)?.Id;
    }

    private async Task ResolveAsync(Alert alert)
    {
        alert.Resolve();

        var linkedNeeds = await needs.ListByAlertIdsAsync([alert.Id]);
        foreach (var need in linkedNeeds)
        {
            need.Complete();
        }
    }
}

public class NotificationDispatcher(
    INotificationChannelRepository channels,
    ISentNotificationRepository sentNotifications,
    IEmailSender emailSender,
    IWhatsAppSender whatsAppSender,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task NotifyAsync(
        Alert alert,
        string productName,
        int minThreshold)
    {
        var active = (await channels.ListAsync())
            .Where(channel => channel.Active)
            .ToList();

        var subject = alert.Type == AlertTypes.LowStock
            ? $"SmartStock - Low stock: {productName}"
            : $"SmartStock - Stock discrepancy: {productName}";

        var text = alert.Type == AlertTypes.LowStock
            ? $"{productName} has {alert.ReferenceStock} units (minimum {minThreshold}). It is time to restock."
            : $"{productName}: the sensor shows {alert.PhysicalStock} units but {alert.RegisteredStock} are registered ({alert.DifferencePct} %).";

        foreach (var channel in active)
        {
            string? error = null;

            try
            {
                if (channel.Channel == NotificationChannel.Email)
                {
                    await emailSender.SendAsync(
                        channel.Destination, subject, $"<p>{text}</p>");
                }
                else
                {
                    await whatsAppSender.SendAsync(
                        channel.Destination, $"{subject}. {text}");
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                logger.LogWarning(
                    exception,
                    "Notification through {Channel} failed",
                    channel.Channel);
            }

            await sentNotifications.AddAsync(
                new SentNotification(
                    alert, channel.Channel, channel.Destination, error));
        }
    }
}




public record AlertResource(
    long Id,
    string Type,
    long ProductId,
    int ReferenceStock,
    string Source,
    string Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    long? NeedId,
    string ProductName,
    int? MinThreshold,
    int? CurrentStock,
    int? MinutesSinceReading,
    int? SensorUnits,
    int? RegisteredStock,
    int? PhysicalStock,
    double? DifferencePct);

public record ChannelResource(
    long Id,
    string Channel,
    string Destination,
    bool Active);

public interface IAlertQueryService
{
    Task<IReadOnlyList<AlertResource>> ListAsync();
}

public record NotifyLowStockCommand(long? ProductId);

public record ChannelChange(long Id, bool Active);

public record UpdateChannelsCommand(
    IReadOnlyList<ChannelChange> Changes);

public record NotificationSentResource(bool Sent, DateTime At);


public interface INotificationService
{
    Task<NotificationSentResource> HandleAsync(
        NotifyLowStockCommand command);

    Task<IReadOnlyList<ChannelResource>> ListChannelsAsync();

    Task<IReadOnlyList<ChannelResource>> HandleAsync(
        UpdateChannelsCommand command);
}

public class AlertQueryService(
    IAlertRepository alerts,
    IRestockingNeedRepository needs,
    IProductCatalogFacade catalog,
    IIoTDeviceFacade iot) : IAlertQueryService
{
    public async Task<IReadOnlyList<AlertResource>> ListAsync()
    {
        var list = await alerts.ListVisibleAsync();

        var products = (await catalog.ListAsync())
            .ToDictionary(product => product.Id);

        var sensors = (await iot.ListAsync())
            .Where(sensor => sensor.ProductId is not null)
            .ToDictionary(sensor => sensor.ProductId!.Value);

        var needByAlert = (
            await needs.ListByAlertIdsAsync(list.Select(alert => alert.Id)))
            .ToDictionary(need => need.AlertId, need => need.Id);

        return list.Select(alert =>
        {
            var product = products.GetValueOrDefault(alert.ProductId);
            var sensor = sensors.GetValueOrDefault(alert.ProductId);
            var online = sensor is { Status: SensorStatuses.Online };

            int? minutesSinceReading =
                online && sensor!.LastReadingAt is { } lastReading
                    ? Math.Max(
                        0,
                        (int)Math.Round(
                            (Clock.UtcNow - lastReading).TotalMinutes))
                    : null;

            int? sensorUnits = online && product is not null
                ? InventoryRules.ToUnits(
                    sensor!.WeightKg, product.UnitWeightKg)
                : null;

            return new AlertResource(
                alert.Id,
                alert.Type,
                alert.ProductId,
                alert.ReferenceStock,
                alert.Source,
                alert.Status,
                alert.CreatedAt,
                alert.ResolvedAt,
                needByAlert.TryGetValue(alert.Id, out var needId)
                    ? needId
                    : null,
                product?.Name ?? string.Empty,
                product?.MinThreshold,
                product?.RegisteredStock,
                minutesSinceReading,
                sensorUnits,
                alert.RegisteredStock,
                alert.PhysicalStock,
                alert.DifferencePct);
        }).ToList();
    }
}

public class NotificationService(
    IAlertRepository alerts,
    IProductCatalogFacade catalog,
    INotificationChannelRepository channels,
    INotificationDispatcher dispatcher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : INotificationService
{
    public async Task<NotificationSentResource> HandleAsync(
        NotifyLowStockCommand command)
    {
        var alert = command.ProductId is { } id
            ? await alerts.FindActiveByProductAsync(id)
            : null;

        if (alert is null)
            throw DomainException.NotFound("ALERT_NOT_FOUND");

        var product = await catalog.FindByIdAsync(alert.ProductId);

        await dispatcher.NotifyAsync(
            alert,
            product?.Name ?? string.Empty,
            product?.MinThreshold ?? 0);

        await unitOfWork.CompleteAsync();

        return new NotificationSentResource(true, Clock.UtcNow);
    }

    public async Task<IReadOnlyList<ChannelResource>> ListChannelsAsync()
    {
        var list = await channels.ListAsync();

        // First use: create the business notification channels.
        if (list.Count == 0 && currentUser.Email is { } email)
        {
            await channels.AddAsync(
                new NotificationChannel(
                    NotificationChannel.Email, email, true));

            await channels.AddAsync(
                new NotificationChannel(
                    NotificationChannel.WhatsApp, string.Empty, false));

            await unitOfWork.CompleteAsync();
            list = await channels.ListAsync();
        }

        return list.Select(ToResource).ToList();
    }

    public async Task<IReadOnlyList<ChannelResource>> HandleAsync(
        UpdateChannelsCommand command)
    {
        var list = await channels.ListAsync();

        var next = list.ToDictionary(
            channel => channel.Id,
            channel => channel.Active);

        foreach (var change in command.Changes)
        {
            if (next.ContainsKey(change.Id))
                next[change.Id] = change.Active;
        }

        // At least one channel must stay active.
        if (!next.Values.Any(active => active))
            throw DomainException.Unprocessable("AT_LEAST_ONE_CHANNEL");

        foreach (var channel in list)
            channel.SetActive(next[channel.Id]);

        await unitOfWork.CompleteAsync();

        return list.Select(ToResource).ToList();
    }

    private static ChannelResource ToResource(
        NotificationChannel channel) =>
        new(
            channel.Id,
            channel.Channel,
            channel.Destination,
            channel.Active);
}