# Integration Health Reporting

GuardLAN records the latest health state for telemetry sources that feed DNS, connection, TLS and IDS data.

The goal is operational visibility: operators should be able to see whether Pi-hole, Zeek and Suricata imports are enabled, reachable and recently checked.

## API

Integration health is exposed through:

```text
GET /api/integrations/health
```

The response contains:

* Summary counts for healthy, warning, unavailable and disabled sources
* Stale source count
* One row per recorded telemetry source
* Last check, last success and last failure timestamps
* Stale-after timestamp
* Records read, imported and rejected
* The last import message
* Recent import runs

## Recorded Sources

The current implementation records:

* Pi-hole DNS imports
* Zeek `conn.log` imports
* Zeek `dns.log` imports
* Zeek `ssl.log` TLS imports
* Suricata Eve JSON alert imports

Health is updated by both manual API-triggered imports and scheduled worker imports because recording happens inside the shared application import services.

## Status Rules

| Status | Meaning |
|---|---|
| Healthy | Source is enabled, available and the latest check had no rejected records. |
| Warning | Source is enabled and available, but the latest check rejected invalid or unmatched records. |
| Unavailable | Source is enabled but the latest check could not read from the source. |
| Disabled | Source is configured off. |
| Stale | Source was previously healthy or warning, but has not checked in for 15 minutes. |

Duplicate records do not make a source unhealthy. They are expected during incremental imports and restart/replay scenarios.

## Storage

Latest health is stored in the `integration_health` table. Recent import runs are stored in the `integration_import_runs` table.

Both tables are managed by EF Core migrations in `GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Migrations`. In development, the API applies pending migrations during startup before seeding sample data.

## UI

The Angular route is:

```text
/integrations
```

The page shows summary cards and a source table with status, source state, record flow, rejection count, last check time and last message.

It also includes manual import buttons for:

* Pi-hole
* Zeek
* Suricata

The lower history table shows the latest recorded import runs.

## Current Implementation

Implemented in:

* [GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationHealth.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationHealth.cs)
* [GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationImportRun.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationImportRun.cs)
* [GuardLAN.API/src/GuardLan.Application/Services/IntegrationHealthService.cs](../GuardLAN.API/src/GuardLan.Application/Services/IntegrationHealthService.cs)
* [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationHealthRepository.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationHealthRepository.cs)
* [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationImportRunRepository.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationImportRunRepository.cs)
* [GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs)
* [GuardLAN.UI/src/app/features/integrations/ui/integrations-page.component.ts](../GuardLAN.UI/src/app/features/integrations/ui/integrations-page.component.ts)
* [GuardLAN.UI/src/app/features/integrations/data-access/integrations.facade.ts](../GuardLAN.UI/src/app/features/integrations/data-access/integrations.facade.ts)

## Next Improvements

* Make freshness thresholds configurable per source type.
* Add filters for import history when the run list grows.
