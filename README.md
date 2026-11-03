# SmartStock - Web Services (RESTful API)

ASP.NET Core 9 · C# · Entity Framework Core 9 · MySQL 8 · JWT · OpenAPI (Scalar)

Startup **InventiaStock** · product **SmartStock** · course 1ASI0730 Aplicaciones Web (NRC 8168).

Full project report: https://github.com/InventiaStock/smartstock-docs

## Run it locally (Ejecutar en local)

1. Install the **.NET 9 SDK** and **MySQL 8** (or Docker Desktop).
2. MySQL with Docker (optional): `docker compose up mysql -d`
3. Run the API:

```bash
dotnet run --project SmartStock.Platform.API
```

4. Open the interactive documentation: http://localhost:5000/scalar
5. Demo login: `store.owner@email.com` / `Secret123` (the data is created on the first run).

## Structure (modular monolith, one folder per bounded context)

```text
SmartStock.Platform.API/
├── Shared/               shared kernel: DbContext, errors, JSON names, contracts between contexts
├── IAM/                  authentication (TS04)
├── ProductCatalog/       products and thresholds (TS13, US05)
├── IoTDevice/            sensors and readings (TS01, TS05, US04)
├── InventoryMonitoring/  Stock (TS02, TS06, TS14, TS15) · Sales (TS07, TS08) · Procurement (TS09 to TS12)
├── AlertsRestocking/     alerts and notifications (TS03)
└── AnalyticsReporting/   dashboard and reports (US15, US25)
```

Every context has the same layers: `Domain`, `Application`, `Infrastructure`, `Interfaces`.

## Secrets

Never commit passwords or keys. Use `dotnet user-secrets` or environment variables
(`ConnectionStrings__Default`, `Jwt__Secret`, `Smtp__Password`, `Twilio__AuthToken`).
