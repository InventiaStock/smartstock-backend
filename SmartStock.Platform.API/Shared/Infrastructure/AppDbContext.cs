using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace SmartStock.Platform.API.Shared.Infrastructure;

/// <summary>
/// The single database context. NOBODY edits this file: every context adds its own
/// IEntityTypeConfiguration class and it is found automatically.
/// (El único DbContext. Nadie lo edita: cada contexto agrega su IEntityTypeConfiguration.)
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : DbContext(options)
{
    /// <summary>Business of the logged user; read by the tenant filter in every query.</summary>
    public long CurrentBusinessId => currentUser.BusinessId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyTenantFilter(modelBuilder);
        ApplySnakeCaseColumns(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // New rows of a business get the business of the logged user (Las filas nuevas toman el negocio del usuario).
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.BusinessId == 0)
                entry.Entity.BusinessId = CurrentBusinessId;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    // WHERE business_id = <logged business> on every entity that implements ITenantEntity.
    private void ApplyTenantFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var entityBusinessId = Expression.Property(parameter, nameof(ITenantEntity.BusinessId));
            var currentBusinessId = Expression.Property(Expression.Constant(this), nameof(CurrentBusinessId));
            var filter = Expression.Lambda(Expression.Equal(entityBusinessId, currentBusinessId), parameter);

            entityType.SetQueryFilter(filter);
        }
    }

    // BusinessId -> business_id (columns in snake_case, as in the database diagram).
    private static void ApplySnakeCaseColumns(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string name) =>
        Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}
