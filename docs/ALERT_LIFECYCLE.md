# GuardLAN Alert Lifecycle

GuardLAN alerts have an explicit review lifecycle in addition to severity and source metadata.

## Statuses

| Status | Meaning |
|---|---|
| `Open` | New or reopened alert that still needs operator attention. |
| `Reviewed` | Alert was triaged, but remains open and still contributes to risk. |
| `Resolved` | Alert was handled and is closed. |
| `FalsePositive` | Alert was closed as benign or expected activity. |
| `Suppressed` | Alert was closed because the operator does not want it treated as active. |

`Resolved`, `FalsePositive` and `Suppressed` set `ResolvedUtc`, so dashboards and device risk scoring no longer count them as open evidence. `Reviewed` keeps `ResolvedUtc` empty because it is acknowledged but still active.

## API

Alert lifecycle actions are exposed through:

```text
PATCH /api/alerts/{id}/review
PATCH /api/alerts/{id}/resolve
PATCH /api/alerts/{id}/false-positive
PATCH /api/alerts/{id}/suppress
PATCH /api/alerts/{id}/reopen
```

Each endpoint accepts an optional body:

```json
{
  "note": "Known test traffic"
}
```

The latest note is stored on the alert, and each action appends a row to `security_alert_history`.

## UI

The Alerts page supports filters for open, reviewed, resolved, false-positive, suppressed and all alerts. Each alert row includes an optional note field and context-aware action buttons.

The dashboard, alerts page and device evidence page refresh on `alertUpdated`, `alertResolved` and `newAlert` live events.
