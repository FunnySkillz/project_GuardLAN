# Device Risk Signals

GuardLAN device risk is intentionally explainable and evidence-based.

The current implementation does not try to prove compromise or run opaque automated classification. It summarizes evidence that already exists in GuardLAN so an operator can decide which devices deserve attention first.

## Inputs

The evaluator uses:

* Open security alerts associated with the device
* Device trust state
* Unknown device type
* First-seen timestamp
* Blocked DNS queries in the rolling 24-hour window
* Recent connection traffic volume in the rolling 24-hour window

The dashboard keeps its DNS widgets scoped to "today", but device risk uses the rolling 24-hour DNS window.

## Levels

Risk levels are derived from a 0 to 100 score:

| Score | Level |
|---:|---|
| 0 | Normal |
| 1-29 | Low |
| 30-54 | Medium |
| 55-79 | High |
| 80-100 | Critical |

The UI treats `Medium`, `High` and `Critical` as elevated risk for filters and summary counts.

## Evidence Rules

The current scoring rules are deliberately simple:

| Signal | Score |
|---|---:|
| Any open critical alert | 80 |
| Any open high-severity alert | 60 |
| Any open medium-severity alert | 35 |
| Device is not trusted | 25 |
| Device type is unknown | 15 |
| Device was first seen in the last 24 hours | 5 if trusted, 20 if untrusted |
| One or more blocked DNS requests | 15 |
| Ten or more blocked DNS requests | 30 |
| Recent traffic is at least 1 GB | 10 |

Scores are capped at 100. Each device DTO includes the calculated level, score and up to four human-readable reasons.

## Current Implementation

Implemented in:

* [GuardLAN.API/src/GuardLan.Application/Services/DeviceRiskEvaluator.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceRiskEvaluator.cs)
* [GuardLAN.API/src/GuardLan.Application/Models/DeviceRiskDto.cs](../GuardLAN.API/src/GuardLan.Application/Models/DeviceRiskDto.cs)
* [GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DeviceService.cs)
* [GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs](../GuardLAN.API/src/GuardLan.Application/Services/DashboardService.cs)
* [GuardLAN.UI/src/app/shared/models/network-device.ts](../GuardLAN.UI/src/app/shared/models/network-device.ts)
* [GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts](../GuardLAN.UI/src/app/features/devices/ui/devices-page.component.ts)
* [GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts](../GuardLAN.UI/src/app/features/dashboard/ui/dashboard-page.component.ts)

## Next Improvements

* Add a device-detail view that shows the supporting alerts, DNS queries and connections behind each risk reason.
* Add review or suppression states so known-benign devices do not stay noisy.
* Tune thresholds after testing with real home-network data.
* Add trend comparison, such as "newly contacted destination" or "higher traffic than normal".
