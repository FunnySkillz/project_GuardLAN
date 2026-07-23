# GuardLAN Zeek Integration

GuardLAN can import structured Zeek log output for connection, DNS and TLS visibility.

References: [Zeek log formats](https://docs.zeek.org/en/current/tutorial/logs.html), [Zeek `conn.log`](https://docs.zeek.org/en/current/reference/logs/conn.html), [Zeek `dns.log`](https://docs.zeek.org/en/current/reference/logs/dns.html) and [Zeek `ssl.log`](https://docs.zeek.org/en/current/reference/logs/ssl.html).

The importer is disabled by default. Enable the logs you want through ASP.NET Core configuration for the API and worker:

```json
{
  "Zeek": {
    "ImportIntervalSeconds": 300,
    "ConnLog": {
      "Enabled": true,
      "Path": "/var/log/zeek/current/conn.log",
      "CheckpointPath": "/var/lib/guardlan/zeek-conn.checkpoint.json",
      "MaxRecords": 5000,
      "ReadFromBeginning": true
    },
    "DnsLog": {
      "Enabled": true,
      "Path": "/var/log/zeek/current/dns.log",
      "CheckpointPath": "/var/lib/guardlan/zeek-dns.checkpoint.json",
      "MaxRecords": 5000,
      "ReadFromBeginning": true
    },
    "SslLog": {
      "Enabled": true,
      "Path": "/var/log/zeek/current/ssl.log",
      "CheckpointPath": "/var/lib/guardlan/zeek-ssl.checkpoint.json",
      "MaxRecords": 5000,
      "ReadFromBeginning": true
    }
  }
}
```

Use environment variables for local or containerized setups:

```powershell
$env:Zeek__ConnLog__Enabled = "true"
$env:Zeek__ConnLog__Path = "D:\zeek\current\conn.log"
$env:Zeek__DnsLog__Enabled = "true"
$env:Zeek__DnsLog__Path = "D:\zeek\current\dns.log"
$env:Zeek__SslLog__Enabled = "true"
$env:Zeek__SslLog__Path = "D:\zeek\current\ssl.log"
```

The API exposes manual import endpoints:

```text
POST http://localhost:5232/api/integrations/zeek/import
POST http://localhost:5232/api/connections/import/zeek
```

The first endpoint imports configured `conn.log`, `dns.log` and `ssl.log` sources together. The second endpoint imports only `conn.log` and is useful while debugging connection ingestion.

The worker also runs the aggregate import on `Zeek:ImportIntervalSeconds`.

## Connection Mapping

The `conn.log` importer reads Zeek tab-separated logs with a `#fields` header. If the header is missing, it falls back to the standard `conn.log` column order.

| Zeek field | GuardLAN field |
|---|---|
| `uid` | `sourceRecordId` |
| `id.orig_h` | `sourceIp` |
| `id.resp_h` | `destinationIp` |
| `id.resp_p` | `destinationPort` |
| `proto` | `protocol` |
| `orig_bytes` | `bytesSent` |
| `resp_bytes` | `bytesReceived` |
| `ts` | `startedUtc` |
| `ts + duration` | `endedUtc` |

Zeek `conn.log` does not usually include a destination domain, so `destinationDomain` is left empty. Domain context comes from DNS imports.

## DNS Mapping

The `dns.log` importer writes into GuardLAN DNS history.

| Zeek field | GuardLAN field |
|---|---|
| `id.orig_h` | `clientIp` |
| `query` | `domain` |
| `ts` | `timestampUtc` |
| `rejected` or `rcode_name = REFUSED` | `wasBlocked` |

Zeek DNS data does not always prove policy blocking. GuardLAN treats rejected or refused DNS rows as blocked-style observations and keeps other query results as allowed.

## TLS Mapping

The `ssl.log` importer writes TLS metadata into `tls_observations`.

| Zeek field | GuardLAN field |
|---|---|
| `uid` | `sourceRecordId` |
| `id.orig_h` | `sourceIp` |
| `id.resp_h` | `destinationIp` |
| `id.resp_p` | `destinationPort` |
| `server_name` | `serverName` |
| `version` | `version` |
| `cipher` | `cipher` |
| `ja3` | `ja3` |
| `ja3s` | `ja3s` |
| `next_protocol` | `alpn` |
| `established` | `wasEstablished` |
| `ts` | `observedUtc` |

TLS observations match known GuardLAN devices by source IP. They match connections by Zeek `uid` when possible and fall back to source device, destination IP, destination port and a five-minute time window.

## Checkpointing

GuardLAN stores a line-number checkpoint after a read has successfully passed through the relevant ingestion service.

If a `CheckpointPath` is empty, the default checkpoint file is written next to the source log as:

```text
<log-file>.guardlan-checkpoint.json
```

Set `ReadFromBeginning` to `false` when connecting GuardLAN to an existing large log and you only want future rows imported. On the first run, GuardLAN records the current line count and starts importing appended rows on later runs.

## Notes

* Duplicate prevention is handled by the normalized DNS, connection and TLS ingestion services.
* Device matching uses the source or client IP against known GuardLAN devices.
* Invalid Zeek rows are skipped and counted in the import result.
* GuardLAN imports structured Zeek output; it does not capture or parse packets directly.
