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
    .AddIamModule(builder.Configuration)
    .AddProductCatalogModule()
    .AddIoTDeviceModule()
    .AddInventoryMonitoringModule()
    .AddAlertsRestockingModule(builder.Configuration)
    .AddAnalyticsReportingModule();

// ── CORS: the Vue frontend and the landing page ──
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ── Authentication (TS04). The other endpoints stay open; with a token each user works with its own business ──
// (Los demás endpoints siguen abiertos; con token cada usuario trabaja con su propio negocio.)
builder.Services.AddHttpContextAccessor();

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

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseMiddleware<ApiErrorMiddleware>();   // outermost: no stack trace ever reaches the client
app.UseCors("Frontend");
app.UseAuthentication();

// A token that is invalid or expired is rejected. Without this, the request would continue with the demo business.
// (Un token inválido o vencido se rechaza; si no, la solicitud seguiría con el negocio demo.)
app.Use(async (context, next) =>
{
    var sendsToken = context.Request.Headers.Authorization.Count > 0;
    var isAuthRoute = context.Request.Path.StartsWithSegments("/api/auth");

    if (sendsToken && !isAuthRoute && context.User.Identity?.IsAuthenticated != true)
        throw new DomainException("UNAUTHORIZED", 401);

    await next(context);
});

app.UseAuthorization();

app.MapOpenApi();                // /openapi/v1.json
app.MapScalarApiReference();     // /scalar/v1  (interactive documentation)
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

await app.InitializeDatabaseAsync();
app.Run();

// Needed by the test project to start the API. (Lo necesita el proyecto de pruebas.)
public partial class Program { }
