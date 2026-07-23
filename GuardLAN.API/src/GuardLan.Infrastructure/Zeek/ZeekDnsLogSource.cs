using GuardLan.Application.Dns;
using GuardLan.Application.Zeek;
using Microsoft.Extensions.Configuration;

namespace GuardLan.Infrastructure.Zeek;

public sealed class ZeekDnsLogSource : IZeekDnsSource
{
    private readonly ZeekLogFileOptions options;
    private readonly ZeekLogFileReader reader;

    public ZeekDnsLogSource(
        IConfiguration configuration,
        ZeekLogFileReader reader)
    {
        options = ZeekLogOptionsReader.Read(configuration, "DnsLog");
        this.reader = reader;
    }

    public bool IsEnabled => options.Enabled;

    public string SourceName => "Zeek dns.log";

    public async Task<ZeekDnsReadResult> ReadNewQueriesAsync(
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
                "trans_id",
                "rtt",
                "query",
                "qclass",
                "qclass_name",
                "qtype",
                "qtype_name",
                "rcode",
                "rcode_name",
                "AA",
                "TC",
                "RD",
                "RA",
                "Z",
                "answers",
                "TTLs",
                "rejected"
            ]),
            ParseQuery,
            cancellationToken);

        return new ZeekDnsReadResult(
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

    private static DnsIngestionRecord? ParseQuery(ZeekLogRow row)
    {
        if (!ZeekValueParser.TryParseTimestamp(row.Read("ts"), out var timestampUtc))
        {
            return null;
        }

        var clientIp = row.Read("id.orig_h");
        var domain = row.Read("query");
        if (clientIp.Length == 0 || domain.Length == 0)
        {
            return null;
        }

        var wasRejected = ZeekValueParser.ParseBoolean(row.Read("rejected"));
        var rcodeName = row.Read("rcode_name");
        var wasBlocked = wasRejected ||
                         rcodeName.Equals("REFUSED", StringComparison.OrdinalIgnoreCase);

        return new DnsIngestionRecord(
            clientIp,
            domain,
            wasBlocked,
            timestampUtc);
    }
}
