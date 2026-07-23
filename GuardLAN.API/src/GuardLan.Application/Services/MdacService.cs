using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class MdacService(
    IUnitOfWork unitOfWork) : IMdacService
{
    public async Task<RegisterDeviceResponse> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var deviceId = Guid.NewGuid();
        var registration = new MdacRegistration
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            DeviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? "Unknown device" : request.DeviceName.Trim(),
            RegisteredUtc = DateTime.UtcNow
        };

        await unitOfWork.MdacRegistrations.AddAsync(registration, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterDeviceResponse(deviceId, "registered");
    }

    public async Task<SubmitSyncResponse> SubmitSyncAsync(SubmitSyncRequest request, CancellationToken cancellationToken)
    {
        var registration = await unitOfWork.MdacRegistrations.GetByDeviceIdAsync(request.DeviceId, cancellationToken);

        if (registration is null)
        {
            return new SubmitSyncResponse("rejected");
        }

        var syncRecord = new MdacSyncRecord
        {
            Id = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            AppName = request.Usage.AppName,
            ForegroundSeconds = request.Usage.ForegroundSeconds,
            SyncedUtc = DateTime.UtcNow
        };

        await unitOfWork.MdacSyncRecords.AddAsync(syncRecord, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitSyncResponse("accepted");
    }

    public async Task<IReadOnlyList<MdacRegistrationSummary>> ListRegistrationsAsync(CancellationToken cancellationToken)
    {
        var registrations = await unitOfWork.MdacRegistrations.GetAllAsync(cancellationToken);

        return registrations
            .OrderByDescending(registration => registration.RegisteredUtc)
            .Select(registration => new MdacRegistrationSummary(
                registration.DeviceId,
                registration.DeviceName,
                registration.RegisteredUtc))
            .ToArray();
    }

    public async Task<IReadOnlyList<MdacSyncRecordSummary>> ListSyncRecordsAsync(CancellationToken cancellationToken)
    {
        var syncRecords = await unitOfWork.MdacSyncRecords.GetAllAsync(cancellationToken);

        return syncRecords
            .OrderByDescending(syncRecord => syncRecord.SyncedUtc)
            .Select(syncRecord => new MdacSyncRecordSummary(
                syncRecord.DeviceId,
                syncRecord.AppName,
                syncRecord.ForegroundSeconds,
                syncRecord.SyncedUtc))
            .ToArray();
    }
}
