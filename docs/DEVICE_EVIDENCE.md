# Device Evidence Drill-Down

GuardLAN device evidence views explain what recent telemetry is available for a single device.

This is the supporting view for device risk signals. Risk reasons stay short in the dashboard and inventory table, while the evidence page shows the recent alerts, DNS queries and network connections behind the device summary.

## API

Device evidence is exposed through:

```text
GET /api/devices/{id}/evidence
```

The endpoint returns one purpose-built DTO:

* Device inventory summary with the current risk level, score and reasons
* Evidence summary for the rolling 24-hour window
* Recent alert rows for the device
* Recent DNS queries for the device
* Recent network connections for the device

The lists are capped to keep the page responsive. Open alerts are included even when they are older than the 24-hour evidence window.

## UI

The Angular route is:

```text
/devices/:id
```

The page shows:

* Device address, type, trust and last-seen state
* Risk level, score and reasons
* Alert, DNS, domain, connection and traffic summary cards
* Recent alert evidence
* Recent DNS activity
* Recent connection destinations

The dashboard and devices table link to this route from each device name.

## Current Implementation

Implemented in:

* [GuardLAN.API/src/GuardLan.Api/Controllers/DevicesController.cs](../GuardLAN.API/src/GuardLan.Api/Controllers/DevicesController.cs)
* [GuardLAN.API/src/GuardLan.Application/Models/DeviceEvidenceDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/DeviceEvidenceDto.cs)
* [GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs)
* [GuardLAN.API/src/GuardLan.Domain/Repositories/IDnsQueryRepository.cs](../GuardLAN.API/src/GuardLan.Domain/Repositories/IDnsQueryRepository.cs)
* [GuardLAN.API/src/GuardLan.Domain/Repositories/INetworkConnectionRepository.cs](../GuardLAN.API/src/GuardLan.Domain/Repositories/INetworkConnectionRepository.cs)
* [GuardLAN.API/src/GuardLan.Domain/Repositories/ISecurityAlertRepository.cs](../GuardLAN.API/src/GuardLan.Domain/Repositories/ISecurityAlertRepository.cs)
* [GuardLAN.UI/src/app/features/devices/ui/device-evidence-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/device-evidence-page.component.ts)
* [GuardLAN.UI/src/app/features/devices/data-access/device-evidence.facade.ts](../GuardLAN.UI/src/app/features/devices/data-access/device-evidence.facade.ts)

## Next Improvements

* Add deep links from alert, DNS and connection rows into dedicated detail views when those views exist.
* Add operator notes or review state for known-benign evidence.
* Add longer-range trend comparison after enough historical data exists.
