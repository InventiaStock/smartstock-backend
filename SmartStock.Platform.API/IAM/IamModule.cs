using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SmartStock.Platform.API.IAM;

public static class IamModule
{
    public static IServiceCollection AddIamModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        services.AddScoped<IDemoSeeder, IamDemoSeeder>();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();

        var section = configuration.GetSection(JwtSettings.Section);
        services.Configure<JwtSettings>(section);
        var jwt = section.Get<JwtSettings>() ?? new JwtSettings();
        var signingKey = jwt.SigningKey();   // fails at startup if the secret is missing

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;   // keep the claim names of the token
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });
        services.AddAuthorization();

        return services;
    }
}