using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using SmartStock.Platform.API.AlertsRestocking;
using SmartStock.Platform.API.AnalyticsReporting;
using SmartStock.Platform.API.IAM;
using SmartStock.Platform.API.InventoryMonitoring;
using SmartStock.Platform.API.IoTDevice;
using SmartStock.Platform.API.ProductCatalog;

var builder = WebApplication.CreateBuilder(args);

// Render and other hosts give the port in the PORT variable. (Render entrega el puerto en PORT.)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── Controllers + JSON (Spanish wire names, see WireNamingPolicy) ──
builder.Services
    .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => WireJson.Configure(options.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = _ => throw new DomainException("INVALID_REQUEST", 400));
builder.Services.ConfigureHttpJsonOptions(options => WireJson.Configure(options.SerializerOptions));

// ── Shared kernel and the six bounded contexts ──
builder.Services.AddSharedKernel(builder.Configuration);
builder.Services
    .AddIamModule()
    .AddProductCatalogModule()
    .AddIoTDeviceModule()
    .AddInventoryMonitoringModule()
    .AddAlertsRestockingModule(builder.Configuration)
    .AddAnalyticsReportingModule();

// ── CORS: the Vue frontend and the landing page ──
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ── Security: JWT. Every endpoint needs the token unless it says [AllowAnonymous] ──
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
          ?? throw new InvalidOperationException("Missing Jwt settings");
if (jwt.Secret.Length < 32) throw new InvalidOperationException("Jwt:Secret needs at least 32 characters");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // keep our short claim names: uid, bid, email
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"code\":\"UNAUTHORIZED\",\"message\":\"Authentication is required.\"}");
            },
        };
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ── OpenAPI documentation (English by default, per the rubric) ──
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "SmartStock Platform API",
            Version = "v1",
            Description = "RESTful Web Services of SmartStock (InventiaStock): inventory monitoring with IoT weight sensors.",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token returned by POST /api/auth/login",
        };
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseMiddleware<ApiErrorMiddleware>();   // outermost: no stack trace ever reaches the client
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();              // /openapi/v1.json
app.MapScalarApiReference().AllowAnonymous();   // /scalar/v1  (interactive documentation)
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();

await app.InitializeDatabaseAsync();
app.Run();

// Needed by the test project to start the API. (Lo necesita el proyecto de pruebas.)
public partial class Program { }
