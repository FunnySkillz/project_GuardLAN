# GuardLAN SignalR Live Updates

GuardLAN uses SignalR to notify the Angular UI when operational data changes.

The browser connects to:

```text
/hubs/guardlan
```

In local Angular development, `proxy.conf.json` forwards `/hubs` to the API at `http://localhost:5232`. In Docker, Nginx forwards `/hubs` to the internal `api` service and preserves the WebSocket upgrade headers.

## Event Contract

The backend broadcasts a `liveUpdate` client event with this shape:

```json
{
  "type": "newAlert",
  "message": "Imported 3 IDS alerts from Suricata eve.json.",
  "createdUtc": "2026-07-23T18:00:00Z",
  "deviceId": null,
  "alertId": null,
  "scanRunId": null,
  "status": null,
  "source": "Suricata eve.json",
  "count": 3
}
```

Supported event types:

| Type | Meaning |
|---|---|
| `scanQueued` | A scan was queued through the API. |
| `scanCompleted` | A queued scan finished successfully. |
| `scanFailed` | A queued scan failed. |
| `newDevice` | A scan discovered a previously unknown device. |
| `deviceStatusChanged` | A known device changed online/offline state. |
| `newAlert` | Scan execution or IDS ingestion created one or more alerts. |
| `alertResolved` | A user resolved an alert. |
| `dnsIngestionCompleted` | A DNS import ran and produced an ingestion result. |

## Publishing Paths

API-hosted services publish through `IHubContext<GuardLanHub>`.

The worker is a separate process, so it publishes by connecting back to the same hub as a SignalR client and invoking `PublishLiveUpdate`. This keeps scan completion, scheduled DNS ingestion and scheduled IDS ingestion visible to connected browsers.

Worker configuration:

```json
{
  "LiveUpdates": {
    "SignalR": {
      "Enabled": true,
      "HubUrl": "http://localhost:5232/hubs/guardlan"
    }
  }
}
```

Docker Compose sets the worker hub URL to:

```text
http://api:8080/hubs/guardlan
```

## Angular Behavior

The Angular app starts one SignalR connection from the app shell.

Feature pages keep their one-request-per-view data model:

* The dashboard refreshes on scan, device, alert and DNS ingestion events.
* The devices page refreshes on new device, device status and scan completion events.
* The DNS page refreshes on DNS ingestion completion.
* The alerts page refreshes on new alert and alert resolution events.

The UI does not currently show a persistent notification feed. Live events are used to refresh the relevant view data.

## Security Notes

The live update hub is intended for trusted local development at this stage. Authentication and authorization are part of the next hardening phase.

Before exposing GuardLAN outside a local network, protect the hub and publish method with the same authentication model as the REST API.
