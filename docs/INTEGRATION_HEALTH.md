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
* One row per recorded telemetry source
* Last check, last success and last failure timestamps
* Records read, imported and rejected
* The last import message

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

Duplicate records do not make a source unhealthy. They are expected during incremental imports and restart/replay scenarios.

## Storage

Health is stored in the `integration_health` table.

GuardLAN does not have EF migration tooling yet, so the repository creates this table with `CREATE TABLE IF NOT EXISTS` before reading or writing health rows. This is intentionally narrow and should be replaced by normal migrations when the database migration phase is implemented.

## UI

The Angular route is:

```text
/integrations
```

The page shows summary cards and a source table with status, source state, record flow, rejection count, last check time and last message.

## Current Implementation

Implemented in:

* [GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationHealth.cs](../GuardLAN.API/src/GuardLan.Domain/Entities/IntegrationHealth.cs)
* [GuardLAN.API/src/GuardLan.Application/Services/IntegrationHealthService.cs](../GuardLAN.API/src/GuardLan.Application/Services/IntegrationHealthService.cs)
* [GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationHealthRepository.cs](../GuardLAN.API/src/GuardLan.Infrastructure/Persistence/Repositories/IntegrationHealthRepository.cs)
* [GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/IntegrationsController.cs)
* [GuardLAN.UI/src/app/features/integrations/ui/integrations-page.component.ts](../GuardLAN.UI/src/app/features/integrations/ui/integrations-page.component.ts)
* [GuardLAN.UI/src/app/features/integrations/data-access/integrations.facade.ts](../GuardLAN.UI/src/app/features/integrations/data-access/integrations.facade.ts)

## Next Improvements

* Add source freshness thresholds so a source can become stale even if the last check succeeded.
* Add manual import buttons from the Integrations page.
* Add import history, not only latest state.
* Replace the table bootstrap with EF migrations.
