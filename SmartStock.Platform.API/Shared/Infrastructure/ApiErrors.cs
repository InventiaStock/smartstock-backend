using System.Text.Json;

namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>
/// Error texts in English (default language of the platform) and Spanish (es_419).
/// The frontend translates by "code", so the code never changes; the message is for developers.
/// (Mensajes de error en inglés y español. El frontend traduce por "code".)
/// </summary>
public static class ErrorMessages
{
    private static readonly Dictionary<string, (string En, string Es)> Messages = new()
    {
        ["UNAUTHORIZED"] = ("Authentication is required.", "Se requiere autenticación."),
        ["INVALID_REQUEST"] = ("The request is not valid.", "La solicitud no es válida."),
        ["INTERNAL_ERROR"] = ("Unexpected error. Please try again later.", "Error inesperado. Inténtalo más tarde."),
        ["NOT_IMPLEMENTED"] = ("This feature is not available yet.", "Esta funcionalidad aún no está disponible."),
        ["NOT_FOUND"] = ("The resource was not found.", "No se encontró el recurso."),
        ["INVALID_CREDENTIALS"] = ("The credentials are incorrect.", "Las credenciales son incorrectas."),
        ["INCOMPLETE"] = ("Some required data is missing.", "Faltan datos obligatorios."),
        ["EMAIL_TAKEN"] = ("The email is already registered.", "El correo ya está registrado."),
        ["PRODUCT_NOT_FOUND"] = ("The product does not exist.", "El producto no existe."),
        ["NO_SALE_PRICE"] = ("The product has no sale price.", "El producto no tiene precio de venta."),
        ["INVALID_QUANTITY"] = ("The quantity must be a whole number greater than zero.", "La cantidad debe ser un entero mayor que cero."),
        ["INSUFFICIENT_STOCK"] = ("There is not enough stock.", "No hay stock suficiente."),
        ["NO_ITEMS"] = ("At least one item is required.", "Se requiere al menos un ítem."),
        ["SUPPLIER_NOT_FOUND"] = ("The supplier does not exist.", "El proveedor no existe."),
        ["DUPLICATE_SUPPLIER"] = ("A supplier with the same name and phone already exists.", "Ya existe un proveedor con el mismo nombre y teléfono."),
        ["ALREADY_RECEIVED"] = ("The purchase was already received.", "La compra ya fue recibida."),
        ["INVALID_PRODUCT"] = ("The product data is not valid.", "Los datos del producto no son válidos."),
        ["THRESHOLD_INVALID"] = ("The minimum threshold must be greater than zero.", "El umbral mínimo debe ser mayor que cero."),
        ["THRESHOLD_EXCEEDS_CAPACITY"] = ("The threshold cannot exceed the maximum capacity.", "El umbral no puede superar la capacidad máxima."),
        ["SENSOR_IN_USE"] = ("The sensor is already linked to another product.", "El sensor ya está vinculado a otro producto."),
        ["PRODUCT_HAS_SENSOR"] = ("The product already has a sensor.", "El producto ya tiene un sensor."),
        ["SENSOR_NOT_FOUND"] = ("The sensor does not exist.", "El sensor no existe."),
        ["INVALID_WEIGHT"] = ("The weight must be zero or greater.", "El peso debe ser cero o mayor."),
        ["ALERT_NOT_FOUND"] = ("There is no active alert for the product.", "No hay una alerta activa para el producto."),
        ["AT_LEAST_ONE_CHANNEL"] = ("At least one notification channel must stay active.", "Debe quedar activo al menos un canal de notificación."),
        ["NO_REGISTERED_QUANTITY"] = ("The comparison cannot be calculated without a registered quantity.", "No se puede calcular la comparación sin una cantidad registrada."),
    };

    public static string Get(string code, string? acceptLanguage)
    {
        if (!Messages.TryGetValue(code, out var pair)) return code;
        var spanish = acceptLanguage?.TrimStart().StartsWith("es", StringComparison.OrdinalIgnoreCase) == true;
        return spanish ? pair.Es : pair.En;
    }
}

/// <summary>
/// Turns every error into JSON { code, message, ...extra } and NEVER shows a stack trace.
/// (Convierte todo error en JSON y nunca muestra el stack trace.)
/// </summary>
public class ApiErrorMiddleware(RequestDelegate next, ILogger<ApiErrorMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            await WriteAsync(context, exception.StatusCode, exception.Code, exception.Extra);
        }
        catch (BadHttpRequestException)
        {
            await WriteAsync(context, 400, "INVALID_REQUEST", null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error in {Path}", context.Request.Path);
            await WriteAsync(context, 500, "INTERNAL_ERROR", null);
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, IDictionary<string, object?>? extra)
    {
        if (context.Response.HasStarted) return;

        var body = new Dictionary<string, object?>
        {
            ["code"] = code,
            ["message"] = ErrorMessages.Get(code, context.Request.Headers.AcceptLanguage.ToString()),
        };

        if (extra is not null)
        {
            foreach (var pair in extra) body[pair.Key] = pair.Value;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
