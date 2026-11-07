namespace SmartStock.Platform.API.AlertsRestocking;

public static class AlertsRestockingModule
{
    public static IServiceCollection AddAlertsRestockingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IRestockingNeedRepository, RestockingNeedRepository>();
        services.AddScoped<INotificationChannelRepository, NotificationChannelRepository>();
        services.AddScoped<ISentNotificationRepository, SentNotificationRepository>();

        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IAlertsFacade, AlertsFacade>();
        services.AddScoped<IAlertQueryService, AlertQueryService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}