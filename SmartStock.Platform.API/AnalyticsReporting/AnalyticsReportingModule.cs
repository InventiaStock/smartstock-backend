namespace SmartStock.Platform.API.AnalyticsReporting;

public static class AnalyticsReportingModule
{
    public static IServiceCollection AddAnalyticsReportingModule(this IServiceCollection services)
    {
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
        return services;
    }
}