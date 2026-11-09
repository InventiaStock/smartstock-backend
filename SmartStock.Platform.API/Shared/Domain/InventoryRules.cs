namespace SmartStock.Platform.API.Shared.Domain;

// Wire values used by the frontend. Keep them exactly as they are.
// (Valores que usa el frontend: no cambiarlos.)
public static class SensorStatuses
{
    public const string Online = "online";
    public const string Disconnected = "disconnected";
    public const string Available = "available";   // sensor without product (sensor sin producto)
    public const string None = "none";             // product without sensor (producto sin sensor)
}

public static class StockLevels
{
    public const string LowStock = "lowStock";
    public const string Healthy = "healthy";
}

public static class MovementTypes
{
    public const string In = "IN";
    public const string Out = "OUT";
}

public static class MovementOrigins
{
    public const string Sale = "SALE";
    public const string Purchase = "PURCHASE";
    public const string Adjustment = "ADJUSTMENT";
}

public static class AlertTypes
{
    public const string LowStock = "LOW_STOCK";
    public const string Discrepancy = "DISCREPANCY";
}

public static class AlertStatuses
{
    public const string Active = "ACTIVE";
    public const string Resolved = "RESOLVED";
}

public static class NeedStatuses
{
    public const string Pending = "PENDING";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
}

public static class PurchaseStatuses
{
    public const string Pending = "PENDING";
    public const string Received = "RECEIVED";
    public const string Cancelled = "CANCELLED";
}

public static class SaleStatuses
{
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
}

public static class Clock
{
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Peru local date (UTC-5, no daylight saving). (Fecha local de Perú.)</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
}

/// <summary>
/// Pure business rules shared by several contexts (R11, R12, R20).
/// (Reglas puras compartidas por varios contextos.)
/// </summary>
public static class InventoryRules
{
    /// <summary>R11: a sensor is online if its last reading is at most 5 minutes old.</summary>
    public static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(5);

    /// <summary>R20: a difference over 10 % between physical and registered stock is a discrepancy.</summary>
    public const double DiscrepancyThresholdPct = 10;

    public static string SensorStatus(long? productId, DateTime? lastReadingAtUtc, DateTime nowUtc)
    {
        if (productId is null) return SensorStatuses.Available;
        if (lastReadingAtUtc is { } last && nowUtc - last <= OnlineWindow) return SensorStatuses.Online;
        return SensorStatuses.Disconnected;
    }

    /// <summary>R12: weight in kg to whole units, using the unit weight of the product.</summary>
    public static int ToUnits(decimal weightKg, decimal unitWeightKg) =>
        unitWeightKg > 0 ? (int)Math.Round(weightKg / unitWeightKg, MidpointRounding.AwayFromZero) : 0;

    /// <summary>Difference in % between physical and registered units (0 if nothing is registered).</summary>
    public static double RawDifferencePct(int physicalUnits, int registeredUnits) =>
        registeredUnits > 0 ? (physicalUnits - registeredUnits) / (double)registeredUnits * 100 : 0;

    public static string StockLevel(int referenceStock, int minThreshold) =>
        referenceStock <= minThreshold ? StockLevels.LowStock : StockLevels.Healthy;

    public static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
