using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Services;
using GuardLan.Application;

namespace GuardLan.Application.Tests;

public class MdacServiceTests
{
    [Fact]
    public async Task RegisterAsync_ReturnsRegisteredDevice()
    {
        var service = new MdacService();

        var response = await service.RegisterAsync(new RegisterDeviceRequest("Pixel 8"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.DeviceId);
        Assert.Equal("registered", response.Status);
    }

    [Fact]
    public async Task SubmitSyncAsync_ReturnsAcceptedForKnownDevice()
    {
        var service = new MdacService();
        var registration = await service.RegisterAsync(new RegisterDeviceRequest("Pixel 8"), CancellationToken.None);

        var response = await service.SubmitSyncAsync(
            new SubmitSyncRequest(registration.DeviceId, new SyncUsage("GuardLAN", 120)),
            CancellationToken.None);

        Assert.Equal("accepted", response.Status);
    }
}
