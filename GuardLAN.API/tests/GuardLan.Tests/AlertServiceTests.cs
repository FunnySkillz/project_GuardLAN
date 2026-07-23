using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Services;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;

namespace GuardLan.Tests;

public sealed class AlertServiceTests
{
    [Fact]
    public async Task MarkFalsePositiveAsyncClosesAlertAndWritesHistory()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var alert = CreateAlert(nowUtc.AddMinutes(-30));
        var unitOfWork = new FakeUnitOfWork(alert);
        var publisher = new CapturingLiveUpdatePublisher();
        var service = new AlertService(
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(nowUtc)),
            publisher);

        var result = await service.MarkFalsePositiveAsync(
            alert.Id,
            new AlertReviewCommand("Known test traffic"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AlertReviewStatus.FalsePositive, alert.ReviewStatus);
        Assert.Equal(nowUtc, alert.ReviewedUtc);
        Assert.Equal(nowUtc, alert.ResolvedUtc);
        Assert.Equal("Known test traffic", alert.ReviewNote);
        Assert.Equal("FalsePositive", alert.History.Single().EventType);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(LiveUpdateTypes.AlertUpdated, publisher.LastUpdate?.Type);
        Assert.Equal("FalsePositive", publisher.LastUpdate?.Status);
    }

    [Fact]
    public async Task ReopenAsyncClearsResolvedUtc()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var alert = CreateAlert(nowUtc.AddHours(-2));
        alert.ReviewStatus = AlertReviewStatus.Resolved;
        alert.ResolvedUtc = nowUtc.AddHours(-1);
        var unitOfWork = new FakeUnitOfWork(alert);
        var publisher = new CapturingLiveUpdatePublisher();
        var service = new AlertService(
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(nowUtc)),
            publisher);

        var result = await service.ReopenAsync(
            alert.Id,
            new AlertReviewCommand(null),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AlertReviewStatus.Open, alert.ReviewStatus);
        Assert.Equal(nowUtc, alert.ReviewedUtc);
        Assert.Null(alert.ResolvedUtc);
        Assert.Null(alert.ReviewNote);
        Assert.Equal("Reopened", alert.History.Single().EventType);
        Assert.Equal(LiveUpdateTypes.AlertUpdated, publisher.LastUpdate?.Type);
        Assert.Equal("Open", publisher.LastUpdate?.Status);
    }

    private static SecurityAlert CreateAlert(DateTime createdUtc)
    {
        return new SecurityAlert
        {
            Id = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            Severity = AlertSeverity.High,
            ReviewStatus = AlertReviewStatus.Open,
            Type = "IdsAlert",
            Message = "Suspicious TLS flow",
            CreatedUtc = createdUtc
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed class CapturingLiveUpdatePublisher : ILiveUpdatePublisher
    {
        public LiveUpdateDto? LastUpdate { get; private set; }

        public Task PublishAsync(LiveUpdateDto update, CancellationToken cancellationToken = default)
        {
            LastUpdate = update;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork(SecurityAlert alert) : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public IDeviceRepository Devices => throw new NotSupportedException();

        public IDnsQueryRepository DnsQueries => throw new NotSupportedException();

        public INetworkConnectionRepository NetworkConnections => throw new NotSupportedException();

        public ITlsObservationRepository TlsObservations => throw new NotSupportedException();

        public INetworkScanRunRepository NetworkScanRuns => throw new NotSupportedException();

        public ISecurityAlertRepository SecurityAlerts { get; } = new FakeSecurityAlertRepository(alert);

        public IIntegrationHealthRepository IntegrationHealth => throw new NotSupportedException();

        public IIntegrationImportRunRepository IntegrationImportRuns => throw new NotSupportedException();

        public IMdacRegistrationRepository MdacRegistrations => throw new NotSupportedException();

        public IMdacSyncRecordRepository MdacSyncRecords => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
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

    private sealed class FakeSecurityAlertRepository(SecurityAlert alert) : ISecurityAlertRepository
    {
        public Task<SecurityAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id == alert.Id ? alert : null);
        }

        public Task<IReadOnlyList<SecurityAlert>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SecurityAlert>>([alert]);
        }

        public Task AddAsync(SecurityAlert entity, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Update(SecurityAlert entity)
        {
        }

        public void Remove(SecurityAlert entity)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<SecurityAlert>> GetSinceAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<SecurityAlert>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<SecurityAlert>> GetOpenAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<SecurityAlert?> GetByIdWithDeviceAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, cancellationToken);
        }

        public Task<IReadOnlyList<SecurityAlert>> GetEvidenceForDeviceAsync(
            Guid deviceId,
            DateTime sinceUtc,
            int limit,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
