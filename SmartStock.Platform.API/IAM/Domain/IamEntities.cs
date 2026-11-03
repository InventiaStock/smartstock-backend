namespace SmartStock.Platform.API.IAM;

/// <summary>The store (tenant). minimarket or bodega. (El negocio.)</summary>
public class Business : IHasId
{
    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string BusinessType { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Business() { }   // for EF Core (para EF Core)

    public Business(string name, string businessType)
    {
        Name = name.Trim();
        BusinessType = businessType;
        CreatedAt = Clock.UtcNow;
    }
}

public class User : IHasId
{
    public long Id { get; private set; }
    public long BusinessId { get; private set; }
    public Business? Business { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(Business business, string email, string passwordHash)
    {
        Business = business;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        CreatedAt = Clock.UtcNow;
    }
}

/// <summary>R4: the reset link expires in 24 hours. Only the hash of the token is stored.</summary>
public class PasswordReset : IHasId
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private PasswordReset() { }

    public PasswordReset(long userId, string tokenHash, TimeSpan lifetime)
    {
        UserId = userId;
        TokenHash = tokenHash;
        IssuedAt = Clock.UtcNow;
        ExpiresAt = IssuedAt.Add(lifetime);
    }
}

public interface IBusinessRepository : IBaseRepository<Business> { }

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByEmailAsync(string email);
}

public interface IPasswordResetRepository : IBaseRepository<PasswordReset> { }
