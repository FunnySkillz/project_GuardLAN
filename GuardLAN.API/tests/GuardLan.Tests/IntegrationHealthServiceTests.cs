using GuardLan.Application.Models;
using GuardLan.Application.Options;
using GuardLan.Application.Services;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace GuardLan.Tests;

public sealed class IntegrationHealthServiceTests
{
    [Fact]
    public async Task RecordAsyncStoresLatestHealthAndImportRun()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var unitOfWork = new FakeUnitOfWork();
        var service = new IntegrationHealthService(
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(nowUtc)),
            Options.Create(new IntegrationHealthOptions()));

        await service.RecordAsync(
            new IntegrationHealthRecord(
                "Pi-hole",
                IntegrationKind.Dns,
                SourceEnabled: true,
                SourceAvailable: true,
                RecordsRead: 10,
                RecordsImported: 8,
                RecordsRejected: 0,
                nowUtc,
                "Imported 8 DNS records from Pi-hole."));

        var overview = await service.GetOverviewAsync();

        Assert.Equal(1, overview.Summary.HealthySources);
        Assert.Equal(0, overview.Summary.StaleSources);
        Assert.Single(overview.Sources);
        Assert.Single(overview.RecentRuns);
        Assert.Equal(nowUtc.AddMinutes(15), overview.Sources[0].StaleAfterUtc);
        Assert.Equal(8, overview.RecentRuns[0].RecordsImported);
    }

    [Fact]
    public async Task GetOverviewAsyncMarksOldSuccessfulSourcesAsStale()
    {
        var lastCheckedUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var nowUtc = lastCheckedUtc.AddMinutes(16);
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.Health.Items.Add(
            new IntegrationHealth
            {
                Id = Guid.NewGuid(),
                Source = "Zeek conn.log",
                Kind = IntegrationKind.Zeek,
                Status = IntegrationHealthStatus.Healthy,
                SourceEnabled = true,
                SourceAvailable = true,
                LastCheckedUtc = lastCheckedUtc,
                LastSuccessUtc = lastCheckedUtc,
                StaleAfterUtc = lastCheckedUtc.AddMinutes(15),
                Message = "No new rows."
            });
        var service = new IntegrationHealthService(
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(nowUtc)),
            Options.Create(new IntegrationHealthOptions()));

        var overview = await service.GetOverviewAsync();

        Assert.Equal(0, overview.Summary.HealthySources);
        Assert.Equal(1, overview.Summary.StaleSources);
        Assert.Equal(IntegrationHealthStatus.Stale, overview.Sources[0].Status);
    }

    [Fact]
    public async Task RecordAsyncUsesConfiguredKindStaleWindow()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var unitOfWork = new FakeUnitOfWork();
        var service = new IntegrationHealthService(
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(nowUtc)),
            Options.Create(
                new IntegrationHealthOptions
                {
                    DefaultStaleAfterMinutes = 15,
                    StaleAfterMinutesByKind = new Dictionary<string, int>
                    {
                        ["Zeek"] = 45
                    }
                }));

        await service.RecordAsync(
            new IntegrationHealthRecord(
                "Zeek conn.log",
                IntegrationKind.Zeek,
                SourceEnabled: true,
                SourceAvailable: true,
                RecordsRead: 1,
                RecordsImported: 1,
                RecordsRejected: 0,
                nowUtc,
                "Imported one connection."));

        var overview = await service.GetOverviewAsync();

        Assert.Equal(nowUtc.AddMinutes(45), overview.Sources[0].StaleAfterUtc);
    }

    [Fact]
    public async Task RecordAsyncLetsSourceOverrideKindStaleWindow()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var unitOfWork = new FakeUnitOfWork();
        var service = new IntegrationHealthService(
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(nowUtc)),
            Options.Create(
                new IntegrationHealthOptions
                {
                    DefaultStaleAfterMinutes = 15,
                    StaleAfterMinutesByKind = new Dictionary<string, int>
                    {
                        ["Zeek"] = 45
                    },
                    StaleAfterMinutesBySource = new Dictionary<string, int>
                    {
                        ["Zeek dns.log"] = 10
                    }
                }));

        await service.RecordAsync(
            new IntegrationHealthRecord(
                "Zeek dns.log",
                IntegrationKind.Zeek,
                SourceEnabled: true,
                SourceAvailable: true,
                RecordsRead: 1,
                RecordsImported: 1,
                RecordsRejected: 0,
                nowUtc,
                "Imported one DNS query."));

        var overview = await service.GetOverviewAsync();

        Assert.Equal(nowUtc.AddMinutes(10), overview.Sources[0].StaleAfterUtc);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeIntegrationHealthRepository Health { get; } = new();

        public FakeIntegrationImportRunRepository Runs { get; } = new();

        public IDeviceRepository Devices => throw new NotSupportedException();

        public IDnsQueryRepository DnsQueries => throw new NotSupportedException();

        public INetworkConnectionRepository NetworkConnections => throw new NotSupportedException();

        public ITlsObservationRepository TlsObservations => throw new NotSupportedException();

        public INetworkScanRunRepository NetworkScanRuns => throw new NotSupportedException();

        public ISecurityAlertRepository SecurityAlerts => throw new NotSupportedException();

        public IIntegrationHealthRepository IntegrationHealth => Health;

        public IIntegrationImportRunRepository IntegrationImportRuns => Runs;

        public IMdacRegistrationRepository MdacRegistrations => throw new NotSupportedException();

        public IMdacSyncRecordRepository MdacSyncRecords => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeIntegrationHealthRepository
        : FakeRepository<IntegrationHealth>,
          IIntegrationHealthRepository
    {
        public Task<IReadOnlyList<IntegrationHealth>> GetAllOrderedAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<IntegrationHealth>>(
                Items
                    .OrderBy(item => item.Kind)
                    .ThenBy(item => item.Source)
                    .ToArray());
        }

        public Task<IntegrationHealth?> GetBySourceAsync(
            string source,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Source == source));
        }

    }

    private sealed class FakeIntegrationImportRunRepository
        : FakeRepository<IntegrationImportRun>,
          IIntegrationImportRunRepository
    {
        public Task<IReadOnlyList<IntegrationImportRun>> GetRecentAsync(
            int limit,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<IntegrationImportRun>>(
                Items
                    .OrderByDescending(item => item.CompletedUtc)
                    .Take(limit)
                    .ToArray());
        }

    }

    private abstract class FakeRepository<TEntity> : IGenericRepository<TEntity>
        where TEntity : class
    {
        public List<TEntity> Items { get; } = [];

        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TEntity>>(Items.ToArray());
        }

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(TEntity entity)
        {
        }

        public void Remove(TEntity entity)
        {
            Items.Remove(entity);
        }
    }
}
