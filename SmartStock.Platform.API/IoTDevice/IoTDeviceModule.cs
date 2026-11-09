namespace SmartStock.Platform.API.IoTDevice;

public static class IoTDeviceModule
{
    public static IServiceCollection AddIoTDeviceModule(this IServiceCollection services)
    {
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<ISensorCommandService, SensorCommandService>();
        services.AddScoped<ISensorQueryService, SensorQueryService>();
        services.AddScoped<IIoTDeviceFacade, IoTDeviceFacade>();   // replaces the stub (reemplaza el stub)
        services.AddScoped<IDemoSeeder, SensorDemoSeeder>();
        return services;
    }
}