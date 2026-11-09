namespace SmartStock.Platform.API.Shared.Domain;

/// <summary>
/// Business-rule error. The API middleware turns it into { code, message, ...extra }.
/// (Error de regla de negocio: el middleware lo convierte en JSON con "code".)
/// </summary>
public class DomainException(string code, int statusCode = 400, IDictionary<string, object?>? extra = null)
    : Exception(code)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
    public IDictionary<string, object?> Extra { get; } = extra ?? new Dictionary<string, object?>();

    public static DomainException BadRequest(string code, IDictionary<string, object?>? extra = null) => new(code, 400, extra);
    public static DomainException NotFound(string code = "NOT_FOUND", IDictionary<string, object?>? extra = null) => new(code, 404, extra);
    public static DomainException Conflict(string code, IDictionary<string, object?>? extra = null) => new(code, 409, extra);
    public static DomainException Unprocessable(string code, IDictionary<string, object?>? extra = null) => new(code, 422, extra);

    /// <summary>Builds the extra data of an error: With(("productoId", 3), ("available", 2)).</summary>
    public static IDictionary<string, object?> With(params (string Key, object? Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
}
