using Microsoft.Extensions.Logging;

namespace SmartStock.Platform.API.Shared.Contracts;

// Default implementations so the API starts while the real classes are still being written.
// Each owner registers the real class later and it replaces the stub (last registration wins).
// (Implementaciones por defecto para que la API arranque mientras se escriben las reales.)

public class NotReadyProductCatalogFacade : IProductCatalogFacade
{
    public Task<ProductSnapshot?> FindByIdAsync(long productId) => Task.FromResult<ProductSnapshot?>(null);
    public Task<IReadOnlyList<ProductSnapshot>> ListAsync() => Task.FromResult<IReadOnlyList<ProductSnapshot>>([]);
    public Task<int> ChangeRegisteredStockAsync(long productId, int delta) => throw new DomainException("NOT_IMPLEMENTED", 501);
}

public class NotReadyIoTDeviceFacade : IIoTDeviceFacade
{
    public Task<SensorSnapshot?> FindByIdAsync(long sensorId) => Task.FromResult<SensorSnapshot?>(null);
    public Task<SensorSnapshot?> FindByProductIdAsync(long productId) => Task.FromResult<SensorSnapshot?>(null);
    public Task<IReadOnlyList<SensorSnapshot>> ListAsync() => Task.FromResult<IReadOnlyList<SensorSnapshot>>([]);
    public Task<IReadOnlyList<ReadingSnapshot>> GetLatestReadingsAsync(long sensorId, int take) => Task.FromResult<IReadOnlyList<ReadingSnapshot>>([]);
}

public class NotReadyStockMovementFacade : IStockMovementFacade
{
    public Task RegisterMovementAsync(RegisterMovementRequest request) => throw new DomainException("NOT_IMPLEMENTED", 501);
    public Task<IReadOnlyList<StockMovementSnapshot>> ListAsync(DateOnly? from, DateOnly? to) => Task.FromResult<IReadOnlyList<StockMovementSnapshot>>([]);
    public Task<IReadOnlyList<StockMovementSnapshot>> ListByOriginAsync(string origin, string originCode) => Task.FromResult<IReadOnlyList<StockMovementSnapshot>>([]);
}

public class NotReadySalesFacade : ISalesFacade
{
    public Task<IReadOnlyList<SaleSnapshot>> ListAsync(DateOnly? from, DateOnly? to) => Task.FromResult<IReadOnlyList<SaleSnapshot>>([]);
}

public class NotReadyProcurementFacade : IProcurementFacade
{
    public Task<IReadOnlyList<PurchaseSnapshot>> ListAsync(DateOnly? from, DateOnly? to) => Task.FromResult<IReadOnlyList<PurchaseSnapshot>>([]);
}

public class NotReadyAlertsFacade : IAlertsFacade
{
    public Task OnStockChangedAsync(long productId, string movementType, int newRegisteredStock) => Task.CompletedTask;
    public Task OnSensorReadingAsync(long productId, decimal weightKg) => Task.CompletedTask;
    public Task OnPurchaseRegisteredAsync(string purchaseCode, long? restockingNeedId) => Task.CompletedTask;
    public Task OnPurchaseReceivedAsync(string purchaseCode, long? restockingNeedId) => Task.CompletedTask;
    public Task<int> CountActiveLowStockAsync() => Task.FromResult(0);
    public Task<long?> GetActiveDiscrepancyAlertIdAsync() => Task.FromResult<long?>(null);
}

/// <summary>Development email sender: only writes the email in the console. (Solo escribe el correo en consola.)</summary>
public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        logger.LogInformation("[EMAIL to {To}] {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

public class NoopWhatsAppSender(ILogger<NoopWhatsAppSender> logger) : IWhatsAppSender
{
    public Task SendAsync(string toPhone, string text)
    {
        logger.LogInformation("[WHATSAPP to {To}] {Text}", toPhone, text);
        return Task.CompletedTask;
    }
}
