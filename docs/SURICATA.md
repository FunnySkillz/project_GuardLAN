# GuardLAN Suricata Integration

GuardLAN can import IDS alerts from Suricata `eve.json` output into the backend alert model.

References: [Suricata Eve JSON format](https://docs.suricata.io/en/latest/output/eve/eve-json-format.html) and [Suricata Eve JSON output](https://docs.suricata.io/en/latest/output/eve/eve-json-output.html).

The importer is disabled by default. Enable it through ASP.NET Core configuration for the API and worker:

```json
{
  "Suricata": {
    "ImportIntervalSeconds": 300,
    "EveLog": {
      "Enabled": true,
      "Path": "/var/log/suricata/eve.json",
      "CheckpointPath": "/var/lib/guardlan/suricata-eve.checkpoint.json",
      "MaxRecords": 5000,
      "ReadFromBeginning": true
    }
  }
}
```

Use environment variables for local or containerized setups:

```powershell
$env:Suricata__EveLog__Enabled = "true"
$env:Suricata__EveLog__Path = "D:\suricata\eve.json"
$env:Suricata__EveLog__CheckpointPath = "D:\suricata\guardlan-eve.checkpoint.json"
```

The API exposes a manual import endpoint:

```text
POST http://localhost:5232/api/integrations/suricata/import
```

The worker also runs the same import on `Suricata:ImportIntervalSeconds`.

## Alert Mapping

Only Eve rows with `event_type` set to `alert` are imported. Flow, DNS, TLS and other Eve row types are ignored by this importer because GuardLAN handles those signals through normalized DNS, connection and TLS ingestion paths.

| Suricata field | GuardLAN field |
|---|---|
| `timestamp` | `createdUtc` |
| `flow_id`, `alert.signature_id`, `timestamp` | `sourceRecordId` |
| `src_ip` | `sourceIp` |
| `dest_ip` | `destinationIp` |
| `dest_port` | `destinationPort` |
| `proto` | `protocol` |
| `alert.signature` | alert message |
| `alert.category` | alert message category |
| `alert.severity` | alert severity |
| `alert.action` | evidence summary |

Suricata alerts match known GuardLAN devices by source IP first and destination IP second. Matching alerts are then associated with stored network connections by endpoint, destination port and a five-minute time window.

## Severity Mapping

GuardLAN maps Suricata alert priority values into its alert severity model:

| Suricata severity | GuardLAN severity |
|---|---|
| `1` or lower | Critical |
| `2` | High |
| `3` | Medium |
| missing or higher | Low |

## Checkpointing

GuardLAN stores a line-number checkpoint after a read has successfully passed through alert ingestion.

If `CheckpointPath` is empty, the default checkpoint file is written next to the source log as:

```text
<eve-json-file>.guardlan-checkpoint.json
```

Set `ReadFromBeginning` to `false` when connecting GuardLAN to an existing large `eve.json` file and you only want future rows imported. On the first run, GuardLAN records the current line count and starts importing appended rows on later runs.

## Notes

* Duplicate prevention uses Suricata source record identity when available and falls back to normalized endpoint, message and timestamp data.
* Alerts without a matching GuardLAN device are skipped so the dashboard stays device-oriented.
* Imported alerts include an initial lifecycle history entry.
* GuardLAN imports structured Suricata output; it does not capture or parse packets directly.
