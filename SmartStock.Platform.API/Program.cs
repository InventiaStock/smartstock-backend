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

// ── No security for now: by indication of the professor the endpoints are open (no login, no token) ──
// (Por ahora los endpoints van abiertos: sin login ni token; el negocio es fijo, ver CurrentUser.)

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

app.MapOpenApi();                // /openapi/v1.json
app.MapScalarApiReference();     // /scalar/v1  (interactive documentation)
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

await app.InitializeDatabaseAsync();
app.Run();

// Needed by the test project to start the API. (Lo necesita el proyecto de pruebas.)
public partial class Program { }
