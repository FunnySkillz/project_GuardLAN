using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IMdacService
{
    Task<RegisterDeviceResponse> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken);

    Task<SubmitSyncResponse> SubmitSyncAsync(SubmitSyncRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<MdacRegistrationSummary>> ListRegistrationsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MdacSyncRecordSummary>> ListSyncRecordsAsync(CancellationToken cancellationToken);
}
