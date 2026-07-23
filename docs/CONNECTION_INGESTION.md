# GuardLAN Connection Ingestion

GuardLAN accepts normalized connection metadata through the backend API.

This endpoint is intentionally source-agnostic. Zeek, firewall exports, router logs or future collectors should normalize their records into this shape before GuardLAN stores them.

```text
POST http://localhost:5232/api/connections/import
```

Example payload:

```json
{
  "source": "Zeek",
  "records": [
    {
      "sourceIp": "192.168.1.22",
      "destinationIp": "140.82.112.4",
      "destinationDomain": "github.com",
      "protocol": "TCP",
      "destinationPort": 443,
      "bytesSent": 488332,
      "bytesReceived": 2138740,
      "startedUtc": "2026-07-23T15:00:00Z",
      "endedUtc": "2026-07-23T15:04:30Z"
    }
  ]
}
```

The ingestion service normalizes and validates records before storage:

* `sourceIp` must match a known GuardLAN device IP.
* `destinationIp` must be a valid IP address.
* `destinationDomain` is optional and normalized to lowercase without a trailing dot.
* `protocol` is normalized to uppercase.
* `destinationPort` must be between `0` and `65535` when present.
* byte counts must be non-negative.
* timestamps are normalized to UTC seconds.
* records with future timestamps, invalid time windows or unknown source devices are skipped.

Duplicate prevention uses the normalized device, destination, protocol, port and start/end timestamps. A repeated import of the same normalized batch should not create duplicate connection rows.

Returned result fields include:

* `recordsRead`
* `imported`
* `skippedDuplicates`
* `skippedInvalid`
* `skippedUnmatchedDevices`
* `matchedDevices`

## Zeek Mapping Target

The first Zeek importer should map `conn.log` data into this contract:

```text
id.orig_h      -> sourceIp
id.resp_h      -> destinationIp
id.resp_p      -> destinationPort
proto          -> protocol
orig_bytes     -> bytesSent
resp_bytes     -> bytesReceived
ts             -> startedUtc
duration + ts  -> endedUtc
```

Domain enrichment can come from DNS correlation when available. GuardLAN should continue ingesting structured Zeek output rather than parsing packets directly inside the application.
