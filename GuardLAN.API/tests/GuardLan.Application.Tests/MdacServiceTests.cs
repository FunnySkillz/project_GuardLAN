using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Services;
using GuardLan.Domain.Repositories;
using GuardLan.Infrastructure.Persistence;
using GuardLan.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GuardLan.Application.Tests;

public class MdacServiceTests
{
    [Fact]
    public async Task RegisterAsync_PersistsRegistration()
    {
        var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<IMdacService>();

        var response = await service.RegisterAsync(new RegisterDeviceRequest("Pixel 8"), CancellationToken.None);

        var repository = provider.GetRequiredService<IMdacRegistrationRepository>();
        var registration = await repository.GetByDeviceIdAsync(response.DeviceId, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.DeviceId);
        Assert.Equal("registered", response.Status);
        Assert.NotNull(registration);
        Assert.Equal("Pixel 8", registration!.DeviceName);
    }

    [Fact]
    public async Task SubmitSyncAsync_ReturnsAcceptedForKnownDevice()
    {
        var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<IMdacService>();
        var registration = await service.RegisterAsync(new RegisterDeviceRequest("Pixel 8"), CancellationToken.None);

        var response = await service.SubmitSyncAsync(
            new SubmitSyncRequest(registration.DeviceId, new SyncUsage("GuardLAN", 120)),
            CancellationToken.None);

        Assert.Equal("accepted", response.Status);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<GuardLanDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IGenericRepository<GuardLan.Domain.Entities.MdacRegistration>, GenericRepository<GuardLan.Domain.Entities.MdacRegistration>>();
        services.AddScoped<IGenericRepository<GuardLan.Domain.Entities.MdacSyncRecord>, GenericRepository<GuardLan.Domain.Entities.MdacSyncRecord>>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IDnsQueryRepository, DnsQueryRepository>();
        services.AddScoped<INetworkConnectionRepository, NetworkConnectionRepository>();
        services.AddScoped<ITlsObservationRepository, TlsObservationRepository>();
        services.AddScoped<INetworkScanRunRepository, NetworkScanRunRepository>();
        services.AddScoped<ISecurityAlertRepository, SecurityAlertRepository>();
        services.AddScoped<IIntegrationHealthRepository, IntegrationHealthRepository>();
        services.AddScoped<IIntegrationImportRunRepository, IntegrationImportRunRepository>();
        services.AddScoped<IMdacRegistrationRepository, MdacRegistrationRepository>();
        services.AddScoped<IMdacSyncRecordRepository, MdacSyncRecordRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMdacService, MdacService>();

        return services.BuildServiceProvider();
    }
}
