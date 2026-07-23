# GuardLAN Zeek Integration

GuardLAN can import connection metadata from a Zeek `conn.log` file into the normalized connection ingestion pipeline.

References: [Zeek log formats](https://docs.zeek.org/en/current/tutorial/logs.html) and [Zeek `conn.log`](https://docs.zeek.org/en/current/reference/logs/conn.html).

The importer is disabled by default. Enable it through ASP.NET Core configuration for the API and worker:

```json
{
  "Zeek": {
    "ConnLog": {
      "Enabled": true,
      "Path": "/var/log/zeek/current/conn.log",
      "CheckpointPath": "/var/lib/guardlan/zeek-conn.checkpoint.json",
      "MaxRecords": 5000,
      "ReadFromBeginning": true,
      "IntervalSeconds": 300
    }
  }
}
```

Use environment variables for local or containerized setups:

```powershell
$env:Zeek__ConnLog__Enabled = "true"
$env:Zeek__ConnLog__Path = "D:\zeek\current\conn.log"
$env:Zeek__ConnLog__CheckpointPath = "D:\zeek\guardlan-conn.checkpoint.json"
```

The API exposes a manual import endpoint:

```text
POST http://localhost:5232/api/connections/import/zeek
```

The worker also runs the same import on `Zeek:ConnLog:IntervalSeconds`.

## Mapping

The importer reads Zeek tab-separated logs with a `#fields` header. If the header is missing, it falls back to the standard `conn.log` column order.

| Zeek field | GuardLAN field |
|---|---|
| `id.orig_h` | `sourceIp` |
| `id.resp_h` | `destinationIp` |
| `id.resp_p` | `destinationPort` |
| `proto` | `protocol` |
| `orig_bytes` | `bytesSent` |
| `resp_bytes` | `bytesReceived` |
| `ts` | `startedUtc` |
| `ts + duration` | `endedUtc` |

Zeek `conn.log` does not usually include a destination domain, so `destinationDomain` is left empty. DNS and TLS enrichment should come from later `dns.log` and TLS metadata imports.

## Checkpointing

GuardLAN stores a line-number checkpoint after a read has successfully passed through the connection ingestion service.

If `CheckpointPath` is empty, the default checkpoint file is written next to the source log as:

```text
conn.log.guardlan-checkpoint.json
```

Set `ReadFromBeginning` to `false` when connecting GuardLAN to an existing large log and you only want future rows imported. On the first run, GuardLAN records the current line count and starts importing appended rows on later runs.

## Notes

* Duplicate prevention is handled by the normalized connection ingestion service.
* Device matching uses the source IP against known GuardLAN devices.
* Invalid Zeek rows are skipped and counted in the import result.
* GuardLAN imports structured Zeek output; it does not capture or parse packets directly.
