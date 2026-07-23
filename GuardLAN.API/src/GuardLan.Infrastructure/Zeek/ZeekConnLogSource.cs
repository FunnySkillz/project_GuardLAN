using GuardLan.Application.Models;
using GuardLan.Application.Zeek;
using Microsoft.Extensions.Configuration;

namespace GuardLan.Infrastructure.Zeek;

public sealed class ZeekConnLogSource : IZeekConnectionSource
{
    private readonly ZeekLogFileOptions options;
    private readonly ZeekLogFileReader reader;

    public ZeekConnLogSource(
        IConfiguration configuration,
        ZeekLogFileReader reader)
    {
        options = ZeekLogOptionsReader.Read(configuration, "ConnLog");
        this.reader = reader;
    }

    public bool IsEnabled => options.Enabled;

    public string SourceName => "Zeek conn.log";

    public async Task<ZeekConnectionReadResult> ReadNewConnectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await reader.ReadNewRowsAsync(
            options,
            SourceName,
            ZeekLogFieldSet.FromFieldNames(
            [
                "ts",
                "uid",
                "id.orig_h",
                "id.orig_p",
                "id.resp_h",
                "id.resp_p",
                "proto",
                "service",
                "duration",
                "orig_bytes",
                "resp_bytes"
            ]),
            ParseConnection,
            cancellationToken);

        return new ZeekConnectionReadResult(
            result.SourceAvailable,
            result.LinesRead,
            result.RecordsParsed,
            result.SkippedInvalid,
            result.Records,
            result.Checkpoint,
            result.Message);
    }

    public Task SaveCheckpointAsync(
        ZeekLogCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        return reader.SaveCheckpointAsync(options, checkpoint, cancellationToken);
    }

    private static ConnectionIngestionRecordDto? ParseConnection(ZeekLogRow row)
    {
        var sourceIp = row.Read("id.orig_h");
        var destinationIp = row.Read("id.resp_h");
        var protocol = row.Read("proto");

        if (sourceIp.Length == 0 ||
            destinationIp.Length == 0 ||
            protocol.Length == 0 ||
            !ZeekValueParser.TryParseTimestamp(row.Read("ts"), out var startedUtc) ||
            !ZeekValueParser.TryParsePort(row.Read("id.resp_p"), out var destinationPort))
        {
            return null;
        }

        var durationSeconds = ZeekValueParser.ParseNullableDouble(row.Read("duration"));
        var endedUtc = durationSeconds is >= 0
            ? startedUtc.AddSeconds(durationSeconds.Value)
            : startedUtc;

        return new ConnectionIngestionRecordDto
        {
            SourceRecordId = row.Read("uid"),
            SourceIp = sourceIp,
            DestinationIp = destinationIp,
            DestinationDomain = null,
            Protocol = protocol,
            DestinationPort = destinationPort,
            BytesSent = ZeekValueParser.ParseNullableLong(row.Read("orig_bytes")) ?? 0,
            BytesReceived = ZeekValueParser.ParseNullableLong(row.Read("resp_bytes")) ?? 0,
            StartedUtc = startedUtc,
            EndedUtc = endedUtc
        };
    }
}
