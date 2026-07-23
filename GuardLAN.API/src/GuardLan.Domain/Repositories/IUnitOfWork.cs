namespace GuardLan.Domain.Repositories;

public interface IUnitOfWork
{
    IDeviceRepository Devices { get; }

    IDnsQueryRepository DnsQueries { get; }

    INetworkConnectionRepository NetworkConnections { get; }

    ITlsObservationRepository TlsObservations { get; }

    INetworkScanRunRepository NetworkScanRuns { get; }

    ISecurityAlertRepository SecurityAlerts { get; }

    IIntegrationHealthRepository IntegrationHealth { get; }

    IIntegrationImportRunRepository IntegrationImportRuns { get; }

    IMdacRegistrationRepository MdacRegistrations { get; }

    IMdacSyncRecordRepository MdacSyncRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
