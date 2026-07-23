using GuardLan.Application.Abstractions;
using GuardLan.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GuardLan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddGuardLanApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<INetworkScanService, NetworkScanService>();

        return services;
    }
}
