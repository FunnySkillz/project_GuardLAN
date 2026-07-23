using GuardLan.Application.Models;
using GuardLan.Application.Services;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;
using GuardLan.Infrastructure.Zeek;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace GuardLan.Tests;

public sealed class ZeekIngestionTests
{
    private static readonly ZeekLogFieldSet ConnectionFields = ZeekLogFieldSet.FromFieldNames(
    [
        "ts",
        "uid",
        "id.orig_h",
        "id.resp_h",
        "id.resp_p",
        "proto"
    ]);

    [Fact]
    public async Task ZeekReaderSkipsMalformedRowsAndContinuesFromCheckpoint()
    {
        using var tempDirectory = new TemporaryDirectory();
        var logPath = Path.Combine(tempDirectory.Path, "conn.log");
        var checkpointPath = Path.Combine(tempDirectory.Path, "conn.checkpoint.json");

        await File.WriteAllLinesAsync(
            logPath,
            [
                "#fields\tts\tuid\tid.orig_h\tid.resp_h\tid.resp_p\tproto",
                "1784800000.000000\tC1\t192.168.1.22\t140.82.112.4\t443\ttcp",
                "not-a-timestamp\tbad\t192.168.1.22\t140.82.112.4\t443\ttcp",
                "1784800001.000000\tC2\t192.168.1.22\t8.8.8.8\t53\tudp"
            ]);

        var options = new ZeekLogFileOptions(
            Enabled: true,
            Path: logPath,
            CheckpointPath: checkpointPath,
            MaxRecords: 100,
            ReadFromBeginning: true);
        var reader = CreateReader();

        var result = await reader.ReadNewRowsAsync(
            options,
            "test conn.log",
            ConnectionFields,
            ParseConnection);

        Assert.True(result.SourceAvailable);
        Assert.Equal(3, result.LinesRead);
        Assert.Equal(2, result.RecordsParsed);
        Assert.Equal(1, result.SkippedInvalid);
        Assert.Equal(4, result.Checkpoint?.LineNumber);

        await reader.SaveCheckpointAsync(options, result.Checkpoint!);
        await File.AppendAllLinesAsync(
            logPath,
            ["1784800002.000000\tC3\t192.168.1.22\t1.1.1.1\t853\ttcp"]);

        var nextResult = await reader.ReadNewRowsAsync(
            options,
            "test conn.log",
            ConnectionFields,
            ParseConnection);

        Assert.Single(nextResult.Records);
        Assert.Equal("C3", nextResult.Records[0].Uid);
        Assert.Equal(5, nextResult.Checkpoint?.LineNumber);
    }

    [Fact]
    public async Task ZeekReaderCanStartAtEndForExistingLargeLogs()
    {
        using var tempDirectory = new TemporaryDirectory();
        var logPath = Path.Combine(tempDirectory.Path, "conn.log");
        var checkpointPath = Path.Combine(tempDirectory.Path, "conn.checkpoint.json");

        await File.WriteAllLinesAsync(
            logPath,
            [
                "#fields\tts\tuid\tid.orig_h\tid.resp_h\tid.resp_p\tproto",
                "1784800000.000000\tC1\t192.168.1.22\t140.82.112.4\t443\ttcp"
            ]);

        var options = new ZeekLogFileOptions(
            Enabled: true,
            Path: logPath,
            CheckpointPath: checkpointPath,
            MaxRecords: 100,
            ReadFromBeginning: false);
        var reader = CreateReader();

        var initialResult = await reader.ReadNewRowsAsync(
            options,
            "test conn.log",
            ConnectionFields,
            ParseConnection);

        Assert.Empty(initialResult.Records);
        Assert.Equal(2, initialResult.Checkpoint?.LineNumber);

        await reader.SaveCheckpointAsync(options, initialResult.Checkpoint!);
        await File.AppendAllLinesAsync(
            logPath,
            ["1784800001.000000\tC2\t192.168.1.22\t8.8.8.8\t53\tudp"]);

        var nextResult = await reader.ReadNewRowsAsync(
            options,
            "test conn.log",
            ConnectionFields,
            ParseConnection);

        Assert.Single(nextResult.Records);
        Assert.Equal("C2", nextResult.Records[0].Uid);
    }

    [Fact]
    public async Task ConnectionIngestionSkipsDuplicateSourceRecordIdsAndInvalidRecords()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var device = new NetworkDevice
        {
            Id = Guid.NewGuid(),
            IpAddress = "192.168.1.22",
            MacAddress = "02:00:00:00:00:22",
            DeviceType = DeviceType.Desktop,
            FirstSeenUtc = nowUtc.AddDays(-2),
            LastSeenUtc = nowUtc,
            IsOnline = true,
            IsTrusted = true
        };
        var connectionRepository = new FakeNetworkConnectionRepository();
        var unitOfWork = new FakeUnitOfWork(
            new FakeDeviceRepository([device]),
            connectionRepository);
        var service = new ConnectionIngestionService(
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(nowUtc)));

        var result = await service.ImportAsync(
            new ConnectionIngestionBatchDto
            {
                Source = "Zeek conn.log",
                Records =
                [
                    CreateConnection("C1", "192.168.1.22", "140.82.112.4", nowUtc.AddMinutes(-2)),
                    CreateConnection("C1", "192.168.1.22", "140.82.112.4", nowUtc.AddMinutes(-1)),
                    CreateConnection("bad", "not-an-ip", "140.82.112.4", nowUtc.AddMinutes(-1))
                ]
            });

        Assert.Equal(3, result.RecordsRead);
        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.SkippedDuplicates);
        Assert.Equal(1, result.SkippedInvalid);
        Assert.Single(connectionRepository.Connections);
        Assert.Equal("Zeek conn.log", connectionRepository.Connections[0].Source);
        Assert.Equal("C1", connectionRepository.Connections[0].SourceRecordId);
    }

    private static ZeekLogFileReader CreateReader()
    {
        return new ZeekLogFileReader(TimeProvider.System, NullLogger<ZeekLogFileReader>.Instance);
    }

    private static ParsedConnection? ParseConnection(ZeekLogRow row)
    {
        if (!double.TryParse(
                row.Read("ts"),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var timestamp) ||
            row.Read("id.orig_h").Length == 0 ||
            row.Read("id.resp_h").Length == 0)
        {
            return null;
        }

        return new ParsedConnection(
            row.Read("uid"),
            row.Read("id.orig_h"),
            DateTime.UnixEpoch.AddSeconds(timestamp));
    }

    private static ConnectionIngestionRecordDto CreateConnection(
        string sourceRecordId,
        string sourceIp,
        string destinationIp,
        DateTime timestampUtc)
    {
        return new ConnectionIngestionRecordDto
        {
            SourceRecordId = sourceRecordId,
            SourceIp = sourceIp,
            DestinationIp = destinationIp,
            Protocol = "tcp",
            DestinationPort = 443,
            BytesSent = 100,
            BytesReceived = 200,
            StartedUtc = timestampUtc,
            EndedUtc = timestampUtc.AddSeconds(5)
        };
    }

    private sealed record ParsedConnection(
        string Uid,
        string SourceIp,
        DateTime StartedUtc);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"guardlan-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed class FakeUnitOfWork(
        IDeviceRepository devices,
        INetworkConnectionRepository networkConnections) : IUnitOfWork
    {
        public IDeviceRepository Devices { get; } = devices;

        public IDnsQueryRepository DnsQueries { get; } = new ThrowingDnsQueryRepository();

        public INetworkConnectionRepository NetworkConnections { get; } = networkConnections;

        public ITlsObservationRepository TlsObservations { get; } = new ThrowingTlsObservationRepository();

        public INetworkScanRunRepository NetworkScanRuns { get; } = new ThrowingNetworkScanRunRepository();

        public ISecurityAlertRepository SecurityAlerts { get; } = new ThrowingSecurityAlertRepository();

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

    private sealed class FakeDeviceRepository(IReadOnlyList<NetworkDevice> devices)
        : ThrowingRepository<NetworkDevice>,
          IDeviceRepository
    {
        public Task<IReadOnlyList<NetworkDevice>> GetInventoryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(devices);
        }

        public Task<IReadOnlyList<NetworkDevice>> GetDevicesForScanUpdateAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<NetworkDevice?> GetByMacAddressAsync(
            string macAddress,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<NetworkDevice>> GetOnlineDevicesAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExistsByMacAddressAsync(
            string macAddress,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeNetworkConnectionRepository
        : ThrowingRepository<NetworkConnection>,
          INetworkConnectionRepository
    {
        public List<NetworkConnection> Connections { get; } = [];

        public override Task AddAsync(NetworkConnection entity, CancellationToken cancellationToken = default)
        {
            Connections.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NetworkConnection>> GetSinceAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<NetworkConnection>>(
                Connections
                    .Where(connection => connection.LastSeenUtc >= sinceUtc)
                    .ToArray());
        }

        public Task<IReadOnlyList<NetworkConnection>> GetSinceWithDevicesAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            return GetSinceAsync(sinceUtc, cancellationToken);
        }

        public Task<NetworkConnectionPage> GetPageSinceWithDevicesAsync(
            NetworkConnectionQuery query,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private abstract class ThrowingRepository<TEntity> : IGenericRepository<TEntity>
        where TEntity : class
    {
        public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public virtual Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public virtual Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public virtual void Update(TEntity entity)
        {
            throw new NotSupportedException();
        }

        public virtual void Remove(TEntity entity)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingDnsQueryRepository
        : ThrowingRepository<DnsQuery>,
          IDnsQueryRepository
    {
        public Task<IReadOnlyList<DnsQuery>> GetSinceAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<DnsQuery>> GetSinceWithDevicesAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingTlsObservationRepository
        : ThrowingRepository<TlsObservation>,
          ITlsObservationRepository
    {
        public Task<IReadOnlyList<TlsObservation>> GetSinceAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingNetworkScanRunRepository
        : ThrowingRepository<NetworkScanRun>,
          INetworkScanRunRepository
    {
        public Task<IReadOnlyList<NetworkScanRun>> GetRecentAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<NetworkScanRun?> GetNextQueuedAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingSecurityAlertRepository
        : ThrowingRepository<SecurityAlert>,
          ISecurityAlertRepository
    {
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
            throw new NotSupportedException();
        }
    }
}
