namespace SmartStock.Platform.API.Shared.Domain;

/// <summary>Every entity has a numeric Id. (Toda entidad tiene Id numérico.)</summary>
public interface IHasId
{
    long Id { get; }
}

/// <summary>
/// Entity that belongs to one business (tenant). The AppDbContext filters it automatically
/// by the business of the logged user. (Entidad de un negocio: se filtra sola por negocio.)
/// </summary>
public interface ITenantEntity
{
    long BusinessId { get; set; }
}

public interface IUnitOfWork
{
    /// <summary>Saves all pending changes in ONE transaction. (Guarda todo en una transacción.)</summary>
    Task CompleteAsync();
}

public interface IBaseRepository<T> where T : class
{
    Task AddAsync(T entity);
    Task<T?> FindByIdAsync(long id);
    Task<IReadOnlyList<T>> ListAsync();
    void Update(T entity);
    void Remove(T entity);
}

/// <summary>Codes like R-00042 or PO-00032. (Códigos tipo R-00042 o PO-00032.)</summary>
public static class CodeFormat
{
    public static string Format(string prefix, int number) => $"{prefix}-{number:D5}";

    public static bool TryParse(string prefix, string? code, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(code) || !code.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase))
            return false;
        return int.TryParse(code[(prefix.Length + 1)..], out number);
    }
}
