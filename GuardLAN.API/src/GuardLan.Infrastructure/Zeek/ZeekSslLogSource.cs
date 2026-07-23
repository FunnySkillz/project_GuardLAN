using GuardLan.Application.Models;
using GuardLan.Application.Zeek;
using Microsoft.Extensions.Configuration;

namespace GuardLan.Infrastructure.Zeek;

public sealed class ZeekSslLogSource : IZeekTlsSource
{
    private readonly ZeekLogFileOptions options;
    private readonly ZeekLogFileReader reader;

    public ZeekSslLogSource(
        IConfiguration configuration,
        ZeekLogFileReader reader)
    {
        options = ZeekLogOptionsReader.Read(configuration, "SslLog");
        this.reader = reader;
    }

    public bool IsEnabled => options.Enabled;

    public string SourceName => "Zeek ssl.log";

    public async Task<ZeekTlsReadResult> ReadNewObservationsAsync(
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
                "version",
                "cipher",
                "curve",
                "server_name",
                "resumed",
                "last_alert",
                "next_protocol",
                "established",
                "cert_chain_fuids",
                "client_cert_chain_fuids",
                "subject",
                "issuer",
                "client_subject",
                "client_issuer",
                "validation_status",
                "ja3",
                "ja3s"
            ]),
            ParseObservation,
            cancellationToken);

        return new ZeekTlsReadResult(
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

    private static TlsObservationIngestionRecordDto? ParseObservation(ZeekLogRow row)
    {
        if (!ZeekValueParser.TryParseTimestamp(row.Read("ts"), out var observedUtc) ||
            !ZeekValueParser.TryParsePort(row.Read("id.resp_p"), out var destinationPort))
        {
            return null;
        }

        var sourceIp = row.Read("id.orig_h");
        var destinationIp = row.Read("id.resp_h");
        if (sourceIp.Length == 0 || destinationIp.Length == 0)
        {
            return null;
        }

        return new TlsObservationIngestionRecordDto
        {
            SourceRecordId = row.Read("uid"),
            SourceIp = sourceIp,
            DestinationIp = destinationIp,
            DestinationPort = destinationPort,
            ServerName = row.Read("server_name"),
            Version = row.Read("version"),
            Cipher = row.Read("cipher"),
            Ja3 = row.Read("ja3"),
            Ja3s = row.Read("ja3s"),
            Alpn = row.Read("next_protocol"),
            WasEstablished = ReadNullableBoolean(row.Read("established")),
            ObservedUtc = observedUtc
        };
    }

    private static bool? ReadNullableBoolean(string value)
    {
        return value.Length == 0 ? null : ZeekValueParser.ParseBoolean(value);
    }
}
