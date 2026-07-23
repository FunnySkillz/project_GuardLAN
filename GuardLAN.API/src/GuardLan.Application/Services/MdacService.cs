using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;

namespace GuardLan.Application.Services;

public sealed class MdacService : IMdacService
{
    private readonly Dictionary<Guid, SubmitSyncRequest> _syncRequests = new();

    public Task<RegisterDeviceResponse> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var deviceId = Guid.NewGuid();
        var response = new RegisterDeviceResponse(deviceId, "registered");

        return Task.FromResult(response);
    }

    public Task<SubmitSyncResponse> SubmitSyncAsync(SubmitSyncRequest request, CancellationToken cancellationToken)
    {
        _syncRequests[request.DeviceId] = request;
        var response = new SubmitSyncResponse("accepted");

        return Task.FromResult(response);
    }
}
