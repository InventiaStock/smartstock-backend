namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>
/// Generic repository. FindByIdAsync uses a normal query (not Find) so the tenant filter always applies.
/// (Repositorio genérico. Usa una consulta normal para que siempre aplique el filtro por negocio.)
/// </summary>
public abstract class BaseRepository<T>(AppDbContext context) : IBaseRepository<T> where T : class, IHasId
{
    protected AppDbContext Context { get; } = context;

    public virtual async Task AddAsync(T entity) => await Context.Set<T>().AddAsync(entity);

    public virtual Task<T?> FindByIdAsync(long id) => Context.Set<T>().FirstOrDefaultAsync(entity => entity.Id == id);

    public virtual async Task<IReadOnlyList<T>> ListAsync() =>
        await Context.Set<T>().OrderBy(entity => entity.Id).ToListAsync();

    public virtual void Update(T entity) => Context.Set<T>().Update(entity);

    public virtual void Remove(T entity) => Context.Set<T>().Remove(entity);
}

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task CompleteAsync() => context.SaveChangesAsync();
}
