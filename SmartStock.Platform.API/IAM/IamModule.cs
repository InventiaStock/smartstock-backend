namespace SmartStock.Platform.API.IAM;

public static class IamModule
{
    public static IServiceCollection AddIamModule(this IServiceCollection services)
    {
        // Only persistence for now: login, register and forgot-password are Sprint 4.
        // (Solo persistencia: login, registro y recuperar contraseña pasan al Sprint 4.)
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        services.AddScoped<IDemoSeeder, IamDemoSeeder>();
        return services;
    }
}
