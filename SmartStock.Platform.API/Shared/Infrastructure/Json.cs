using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>
/// The code uses English names (course convention) but the frontend already consumes Spanish JSON
/// names (documented OpenAPI contract). This table is the ONE place where both worlds meet.
/// Names not listed here only change to camelCase (id, email, token, total, items...).
/// (El código usa inglés; el JSON usa los nombres en español que ya consume el frontend.)
/// </summary>
public class WireNamingPolicy : JsonNamingPolicy
{
    private static readonly Dictionary<string, string> Map = new()
    {
        // products
        ["Name"] = "nombre", ["Category"] = "categoria", ["UnitWeight"] = "pesoUnitario",
        ["SalePrice"] = "precioVenta", ["PurchaseCost"] = "costoCompra", ["UsualSupplier"] = "proveedorHabitual",
        ["RegisteredStock"] = "stockRegistrado", ["MinThreshold"] = "umbralMinimo", ["MaxCapacity"] = "capacidadMaxima",
        ["SensorStatus"] = "estadoSensor", ["SensorUnits"] = "unidadesSensor", ["ReferenceStock"] = "stockReferencia",
        ["StockLevel"] = "nivelStock", ["ProductId"] = "productoId", ["ProductName"] = "productoNombre",
        // sensors
        ["Code"] = "codigo", ["Status"] = "estado", ["WeightKg"] = "pesoKg", ["Weight"] = "peso",
        ["LastReadingAt"] = "ultimaLecturaAt", ["ReferenceWeightKg"] = "pesoReferenciaKg", ["Units"] = "unidades",
        ["ReadAt"] = "fecha", ["Readings"] = "lecturas", ["Sensor"] = "sensor", ["SensorId"] = "sensorId",
        // sales, purchases, movements
        ["Date"] = "fecha", ["Quantity"] = "cantidad", ["UnitPrice"] = "precioUnitario", ["UnitCost"] = "costoUnitario",
        ["Subtotal"] = "subtotal", ["Sales"] = "ventas", ["Purchases"] = "compras", ["Movements"] = "movimientos",
        ["SupplierId"] = "proveedorId", ["SupplierName"] = "proveedorNombre", ["Phone"] = "telefono",
        ["ContactEmail"] = "correo", ["Type"] = "tipo", ["Origin"] = "origen", ["OriginCode"] = "origenId",
        // comparison
        ["PhysicalUnits"] = "unidadesFisicas", ["DifferencePct"] = "diferenciaPct", ["Result"] = "resultado",
        ["Discrepancies"] = "discrepancias", ["AlertId"] = "alertaId", ["Product"] = "producto",
        // alerts and channels
        ["Source"] = "fuente", ["CreatedAt"] = "creadaEn", ["ResolvedAt"] = "resueltaEn", ["NeedId"] = "necesidadId",
        ["CurrentStock"] = "stockActual", ["MinutesSinceReading"] = "minutosDesdeLectura", ["PhysicalStock"] = "stockFisico",
        ["Channel"] = "canal", ["Destination"] = "destino", ["Active"] = "activo",
        // dashboard and reports
        ["TodaySales"] = "ventasHoy", ["TodayPurchases"] = "comprasHoy", ["Count"] = "cantidad",
        ["LowStockAlerts"] = "alertasStockBajo", ["RecentActivity"] = "actividadReciente", ["StockSummary"] = "resumenStock",
        ["Level"] = "nivel", ["From"] = "desde", ["To"] = "hasta", ["SalesTotal"] = "ventasTotal",
        ["PurchasesTotal"] = "comprasTotal", ["PendingPurchases"] = "comprasPendientes",
    };

    public override string ConvertName(string name) =>
        Map.TryGetValue(name, out var wire) ? wire : char.ToLowerInvariant(name[0]) + name[1..];
}

/// <summary>Dates are always UTC with "Z", so the browser shows the right hour. (Fechas siempre en UTC con Z.)</summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
    }
}

public static class WireJson
{
    /// <summary>Applied to MVC and to the OpenAPI generator, so the documentation shows the real names.</summary>
    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = new WireNamingPolicy();
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new UtcDateTimeJsonConverter());
    }
}
