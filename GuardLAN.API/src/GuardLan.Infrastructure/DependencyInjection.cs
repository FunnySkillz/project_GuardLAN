using GuardLan.Application.Scanning;
using GuardLan.Infrastructure.Persistence;
using GuardLan.Infrastructure.Persistence.Repositories;
using GuardLan.Infrastructure.Scanning;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GuardLan.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=guardlan;Username=guardlan;Password=guardlan";

    public static IServiceCollection AddGuardLanInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GuardLanDb") ?? DefaultConnectionString;

        services.AddDbContext<GuardLanDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IDnsQueryRepository, DnsQueryRepository>();
        services.AddScoped<INetworkConnectionRepository, NetworkConnectionRepository>();
        services.AddScoped<INetworkScanRunRepository, NetworkScanRunRepository>();
        services.AddScoped<ISecurityAlertRepository, SecurityAlertRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<INetworkScanner, NmapNetworkScanner>();

        return services;
    }
}
