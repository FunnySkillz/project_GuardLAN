using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace GuardLan.Infrastructure.Persistence;

public sealed class UnitOfWork(
    GuardLanDbContext dbContext,
    IDeviceRepository devices,
    IDnsQueryRepository dnsQueries,
    INetworkConnectionRepository networkConnections,
    ITlsObservationRepository tlsObservations,
    INetworkScanRunRepository networkScanRuns,
    ISecurityAlertRepository securityAlerts) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public IDeviceRepository Devices { get; } = devices;

    public IDnsQueryRepository DnsQueries { get; } = dnsQueries;

    public INetworkConnectionRepository NetworkConnections { get; } = networkConnections;

    public ITlsObservationRepository TlsObservations { get; } = tlsObservations;

    public INetworkScanRunRepository NetworkScanRuns { get; } = networkScanRuns;

    public ISecurityAlertRepository SecurityAlerts { get; } = securityAlerts;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A database transaction is already active.");
        }

        _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active database transaction exists.");
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
