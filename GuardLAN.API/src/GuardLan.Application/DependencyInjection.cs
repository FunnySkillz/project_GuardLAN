using GuardLan.Application.Abstractions;
using GuardLan.Application.Scanning;
using GuardLan.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GuardLan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddGuardLanApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.TryAddScoped<ILiveUpdatePublisher, NoOpLiveUpdatePublisher>();
        services.AddScoped<IConnectionIngestionService, ConnectionIngestionService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IIdsAlertIngestionService, IdsAlertIngestionService>();
        services.AddScoped<ISuricataAlertImportService, SuricataAlertImportService>();
        services.AddScoped<ITlsObservationIngestionService, TlsObservationIngestionService>();
        services.AddScoped<IZeekImportService, ZeekImportService>();
        services.AddScoped<IZeekConnectionImportService, ZeekConnectionImportService>();
        services.AddScoped<IZeekDnsImportService, ZeekDnsImportService>();
        services.AddScoped<IZeekTlsImportService, ZeekTlsImportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IDnsRecordIngestionService, DnsRecordIngestionService>();
        services.AddScoped<IDnsService, DnsService>();
        services.AddScoped<IDnsIngestionService, DnsIngestionService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<INetworkScanService, NetworkScanService>();
        services.AddScoped<IScanExecutionService, ScanExecutionService>();

        return services;
    }
}
