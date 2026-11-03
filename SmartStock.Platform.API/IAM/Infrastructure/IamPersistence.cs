namespace SmartStock.Platform.API.IAM;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("businesses");
        builder.HasKey(business => business.Id);
        builder.Property(business => business.Name).IsRequired().HasMaxLength(120);
        builder.Property(business => business.BusinessType).IsRequired().HasMaxLength(20);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).IsRequired().HasMaxLength(160);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(100);
        builder.HasIndex(user => user.Email).IsUnique();   // R1
        builder.HasOne(user => user.Business).WithMany()
            .HasForeignKey(user => user.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PasswordResetConfiguration : IEntityTypeConfiguration<PasswordReset>
{
    public void Configure(EntityTypeBuilder<PasswordReset> builder)
    {
        builder.ToTable("password_resets");
        builder.HasKey(reset => reset.Id);
        builder.Property(reset => reset.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasOne<User>().WithMany().HasForeignKey(reset => reset.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BusinessRepository(AppDbContext context) : BaseRepository<Business>(context), IBusinessRepository { }

public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    public Task<User?> FindByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Context.Set<User>().FirstOrDefaultAsync(user => user.Email == normalized);
    }
}

public class PasswordResetRepository(AppDbContext context)
    : BaseRepository<PasswordReset>(context), IPasswordResetRepository { }

/// <summary>Demo accounts of the Fake API: store.owner@email.com and bodega.owner@email.com (Secret123).</summary>
public class IamDemoSeeder : IDemoSeeder
{
    public int Order => 10;

    public async Task SeedAsync(AppDbContext db, DemoSeedContext context)
    {
        if (await db.Set<User>().AnyAsync())
        {
            var existing = await db.Set<Business>().OrderBy(business => business.Id).ToListAsync();
            if (existing.Count > 0) context.MinimarketBusinessId = existing[0].Id;
            if (existing.Count > 1) context.BodegaBusinessId = existing[1].Id;
            return;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword("Secret123");
        var minimarket = new Business("Fresh Corner", "minimarket");
        var bodega = new Business("Bodega Don Pepe", "bodega");

        db.Set<User>().Add(new User(minimarket, "store.owner@email.com", hash));
        db.Set<User>().Add(new User(bodega, "bodega.owner@email.com", hash));
        await db.SaveChangesAsync();

        context.MinimarketBusinessId = minimarket.Id;
        context.BodegaBusinessId = bodega.Id;
    }
}
