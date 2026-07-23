# MDAC - Mobile Device Activity Collector

MDAC is the GuardLAN mobile companion project.

This document describes the architecture and implementation plan for a voluntary Android and iOS application that collects authorized mobile-device activity and synchronizes it with GuardLAN.

## 1. Purpose

MDAC is a companion application installed voluntarily on monitored Android and iOS devices.

Its purpose is to collect authorized device-usage information and securely synchronize it with the GuardLAN backend.

MDAC complements GuardLAN’s network monitoring.

```text
Network monitoring
    -> Which services and destinations produced traffic?

Mobile agent
    -> Which applications were active on the device?

GuardLAN backend
    -> Correlates both data sources

Angular dashboard
    -> Presents the combined activity timeline
```

The application must be visible, consent-based and removable by the device owner.

It must not operate as a hidden surveillance application.

---

# 2. Primary Objectives

The mobile agent should provide GuardLAN with:

* Registered mobile-device identity
* Installed application metadata where permitted
* Application foreground usage
* Application usage totals
* Application activity sessions
* Screen-on and screen-off periods where available
* Device unlock activity where available
* Agent health and synchronization status
* Permission status
* Operating-system restrictions
* Local collection errors

GuardLAN should use this data to display:

* Daily application usage
* Weekly application usage
* Most-used applications
* Application usage by device
* Application usage timelines
* Usage trends
* Probable video-streaming sessions
* Probable voice or video calls
* Differences between foreground usage and network activity
* Agent permission and synchronization health

---

# 3. Important Technical Decision

## React Native is the application shell

The following functionality should be shared between Android and iOS:

* User interface
* Navigation
* Device registration
* GuardLAN server configuration
* Authentication
* Local application settings
* Permission explanations
* Synchronization state
* API client
* Request and response DTOs
* Local queue coordination
* Error presentation
* Diagnostics
* Manual synchronization
* Application usage dashboard
* Privacy controls

## Native modules provide operating-system access

The following functionality cannot be implemented reliably through pure React Native JavaScript:

* Reading Android usage statistics
* Opening Android usage-access settings
* Running Android WorkManager jobs
* Accessing iOS Family Controls
* Accessing iOS Device Activity
* Running iOS Device Activity extensions
* Reading platform-specific authorization state
* Secure platform key storage where special behavior is required
* Collecting usage while the React Native runtime is not running

The application therefore requires:

```text
React Native and TypeScript
    |
Typed Turbo Native Module specification
    |
    |-- Kotlin implementation for Android
    |
    |-- Swift implementation for iOS
```

React Native Codegen should generate the typed platform interfaces from a TypeScript specification. The Android and iOS implementations then provide the platform-specific behavior.

---

# 4. Recommended Technology Stack

## Shared application

* React Native
* TypeScript
* React Navigation or Expo Router
* TanStack Query for server state
* Zustand for limited client state
* React Hook Form
* Zod for runtime API validation
* Axios or a centralized Fetch-based API client
* MMKV or SQLite for local application state
* Native secure storage for credentials
* Jest
* React Native Testing Library
* Maestro or Detox for end-to-end tests

## Android implementation

* Kotlin
* `UsageStatsManager`
* `UsageEvents`
* WorkManager
* Room or native SQLite access
* Android Keystore
* PackageManager
* AppOpsManager
* BroadcastReceiver where necessary

Android exposes usage-statistics APIs through `UsageStatsManager`. Background synchronization should use WorkManager rather than relying on an indefinitely running React Native process.

## iOS implementation

* Swift
* FamilyControls
* DeviceActivity
* ManagedSettings where required
* Device Activity Monitor extension
* Device Activity Report extension where required
* App Groups
* Keychain
* BackgroundTasks for permitted synchronization work

Apple’s Screen Time functionality is based on Family Controls, Managed Settings and Device Activity. The application and relevant extensions require the Family Controls capability and entitlements.

## Build environment

The project may use Expo tooling, but it cannot remain an Expo Go-only project.

Recommended:

```text
Expo development tools
Expo Router
Expo prebuild
Custom development client
EAS Build or native local builds
Custom native modules
```

Expo Go cannot include project-specific native Kotlin and Swift modules. A custom development client and native projects are required once these integrations are introduced. React Native’s official guidance supports using Expo tooling while adding custom native code through a development client.

---

# 5. High-Level Architecture

```text
MDAC - Mobile Device Activity Collector
│
├── React Native application
│   ├── Onboarding
│   ├── Server pairing
│   ├── Permission setup
│   ├── Usage overview
│   ├── Synchronization status
│   ├── Diagnostics
│   └── Privacy controls
│
├── NativeUsageModule
│   ├── Android Kotlin implementation
│   └── iOS Swift implementation
│
├── NativeSchedulerModule
│   ├── Android WorkManager
│   └── iOS permitted background scheduling
│
├── Local secure storage
│
├── Local usage database
│
├── Synchronization queue
│
└── GuardLAN API client
```

Backend architecture:

```text
Mobile agent
    |
    | HTTPS and device authentication
    v
GuardLAN API
    |
Agent ingestion service
    |
Validation and deduplication
    |
Usage normalization
    |
PostgreSQL
    |
Activity-correlation service
    |
Angular dashboard
```

---

# 6. Platform Capability Matrix

| Capability                          |                              Android |                                         iOS |
| ----------------------------------- | -----------------------------------: | ------------------------------------------: |
| Shared React Native UI              |                                  Yes |                                         Yes |
| Device registration                 |                                  Yes |                                         Yes |
| Manual synchronization              |                                  Yes |                                         Yes |
| Periodic background synchronization |                                  Yes |                                  Restricted |
| Application foreground totals       |                                  Yes |                 Limited by Screen Time APIs |
| Detailed foreground transitions     |                   Generally possible |                   Not equivalent to Android |
| Installed application names         | Generally possible with restrictions |                          Privacy-restricted |
| Exact package or bundle identity    |                   Generally possible | May be represented through protected tokens |
| Usage while app is closed           |           Yes, through OS statistics |   Through Screen Time frameworks/extensions |
| Background JavaScript process       |                      Not relied upon |                             Not relied upon |
| Native implementation required      |                                  Yes |                                         Yes |
| Special entitlement required        |              Usage Access permission |                 Family Controls entitlement |
| App Store approval uncertainty      |                                Lower |                                      Higher |

The shared application must expose one normalized GuardLAN interface while allowing platform-specific capability differences.

The backend must not assume that every Android field exists on iOS.

---

# 7. Core User Flows

## 7.1 First launch

```text
Open GuardLAN Agent
    |
Explain what data is collected
    |
Accept privacy notice
    |
Connect to GuardLAN server
    |
Pair device
    |
Grant operating-system permissions
    |
Run initial capability check
    |
Perform initial synchronization
    |
Show agent health screen
```

## 7.2 Server pairing

The user opens GuardLAN’s Angular dashboard and requests a device pairing code.

Example:

```text
GuardLAN dashboard:
Pair new mobile agent

Pairing code:
R7X4-KP29

Expires:
10 minutes
```

The mobile app sends:

```json
{
  "pairingCode": "R7X4-KP29",
  "deviceName": "Personal Phone",
  "platform": "Android",
  "operatingSystemVersion": "16",
  "agentVersion": "1.0.0"
}
```

The server returns:

```json
{
  "agentId": "019c...",
  "deviceId": "019c...",
  "accessToken": "...",
  "refreshToken": "...",
  "configuration": {
    "syncIntervalMinutes": 60,
    "retentionDays": 30,
    "collectApplicationUsage": true
  }
}
```

The credentials must be stored in Android Keystore or iOS Keychain-backed secure storage.

## 7.3 Permission setup

The application must explain why each permission is needed before opening the operating-system permission screen.

Example:

```text
Application usage access

GuardLAN needs access to application usage statistics to calculate how
long applications remain active on this phone.

GuardLAN does not read messages, call audio, passwords or application content.
```

The permission screen must show:

* Current status
* Why it is needed
* What becomes unavailable without it
* A button to open system settings
* A button to recheck the permission
* A link to collected-data details

## 7.4 Normal operation

```text
Operating system records usage
    |
Native collector queries new usage
    |
Collector normalizes events
    |
Events are stored locally
    |
Aggregator creates sessions and totals
    |
Sync worker creates a batch
    |
Batch is uploaded to GuardLAN
    |
Server acknowledges accepted records
    |
Acknowledged local records are marked synchronized
```

## 7.5 Permission revoked

```text
Usage permission removed
    |
Native module detects missing permission
    |
Collection stops
    |
Agent health becomes degraded
    |
Backend receives permission-status update
    |
GuardLAN dashboard displays warning
```

The application must never repeatedly force the permission screen open.

---

# 8. React Native Application Screens

## 8.1 Welcome screen

Purpose:

* Introduce GuardLAN
* Explain authorized monitoring
* State what the agent does not collect
* Start pairing

Content:

```text
GuardLAN Agent

Connect this phone to your GuardLAN installation to monitor authorized
application usage and correlate it with local network activity.

GuardLAN does not read messages, record calls, capture passwords or inspect
the contents of encrypted applications.
```

## 8.2 Server connection screen

Fields:

* GuardLAN server address
* Pairing code
* Optional manually entered device name

Actions:

* Test connection
* Pair device
* Display TLS or certificate errors clearly

The production application must not permit insecure HTTP except for an explicitly enabled local development mode.

## 8.3 Permission screen

Display each capability separately:

```text
Application usage access        Granted
Background synchronization      Available
Notifications                   Optional
Battery optimization            Restricted
```

Each item must include:

* State
* Explanation
* Required or optional indicator
* Resolution action

## 8.4 Agent overview

Display:

* Device name
* Connection state
* Last collection time
* Last successful synchronization
* Pending records
* Today’s collected usage
* Permission state
* Agent version
* Server address

## 8.5 Local application usage

Display the phone owner’s own collected data:

* Today’s total usage
* Usage per application
* Session timeline
* Last synchronized state
* Locally pending records

The local view makes the monitoring transparent and allows accuracy testing.

## 8.6 Synchronization diagnostics

Display:

* Last attempted synchronization
* Last successful synchronization
* Last server response
* Pending batches
* Number of rejected records
* Current authentication status
* Background-worker status
* Local database size

Actions:

* Synchronize now
* Retry failed records
* Export sanitized diagnostics
* Re-register device after explicit confirmation

## 8.7 Privacy and collection settings

Settings:

* Enable or disable collection
* Enable or disable synchronization
* View collected data
* Delete locally stored data
* Request deletion from GuardLAN
* Unpair device
* Display retention policy
* Display GuardLAN server identity

---

# 9. Shared TypeScript Architecture

Recommended structure:

```text
src/
├── app/
│   ├── navigation/
│   ├── providers/
│   └── bootstrap/
│
├── features/
│   ├── onboarding/
│   ├── pairing/
│   ├── permissions/
│   ├── usage/
│   ├── synchronization/
│   ├── diagnostics/
│   └── settings/
│
├── core/
│   ├── api/
│   ├── authentication/
│   ├── configuration/
│   ├── storage/
│   ├── logging/
│   └── errors/
│
├── native/
│   ├── usage/
│   ├── scheduler/
│   └── device/
│
├── shared/
│   ├── components/
│   ├── hooks/
│   ├── models/
│   └── utilities/
│
└── theme/
```

Feature structure:

```text
features/usage/
├── api/
├── components/
├── hooks/
├── models/
├── screens/
├── services/
└── validation/
```

---

# 10. State Management

## Server state

Use TanStack Query for:

* Agent details
* Server configuration
* Synchronization responses
* Remote application metadata
* Remote usage summaries
* Agent revocation state

## Local client state

Use Zustand only for limited application state such as:

* Selected GuardLAN server
* Onboarding completion
* Current permission summary
* UI preferences
* Current pairing flow

Do not duplicate server state inside Zustand.

## Native usage state

Collected events belong in the local database, not in global React state.

React state may expose only:

* Current summary
* Recent sessions
* Pending count
* Last collection result

## Forms

Use React Hook Form and Zod for:

* Pairing
* Server address
* Device naming
* Diagnostic configuration
* Privacy preferences

---

# 11. Native Module Contract

Create a typed Turbo Native Module.

Example TypeScript specification:

```typescript
import type { TurboModule } from 'react-native';
import { TurboModuleRegistry } from 'react-native';

export type NativePermissionStatus =
  | 'notDetermined'
  | 'granted'
  | 'denied'
  | 'restricted'
  | 'unsupported';

export type NativeUsageEvent = {
  eventId: string;
  applicationIdentifier: string;
  applicationDisplayName: string | null;
  eventType: string;
  timestampUtc: string;
};

export type NativeUsageAggregate = {
  applicationIdentifier: string;
  applicationDisplayName: string | null;
  periodStartUtc: string;
  periodEndUtc: string;
  foregroundSeconds: number;
};

export interface Spec extends TurboModule {
  isSupported(): boolean;

  getPermissionStatus(): Promise<NativePermissionStatus>;

  requestPermission(): Promise<NativePermissionStatus>;

  openPermissionSettings(): Promise<void>;

  collectUsageEvents(
    fromUtc: string,
    toUtc: string
  ): Promise<readonly NativeUsageEvent[]>;

  collectUsageAggregates(
    fromUtc: string,
    toUtc: string
  ): Promise<readonly NativeUsageAggregate[]>;

  getCollectorDiagnostics(): Promise<string>;
}

export default TurboModuleRegistry.getEnforcing<Spec>(
  'GuardLanUsage'
);
```

The JavaScript API must remain platform-neutral.

Platform-specific differences should be returned as capabilities:

```typescript
export interface UsageCapabilities {
  usageCollectionSupported: boolean;
  detailedSessionsSupported: boolean;
  applicationNamesSupported: boolean;
  backgroundCollectionSupported: boolean;
  installedApplicationsSupported: boolean;
}
```

---

# 12. Android Native Implementation

## 12.1 Permission

Android usage collection requires the user to explicitly grant usage access.

The application should:

1. Check AppOps state.
2. Explain the permission.
3. Open the operating system’s usage-access settings.
4. Recheck after returning.
5. Report the status to GuardLAN.

The application must not claim that permission was granted simply because the settings screen was opened.

## 12.2 Usage collection

The Android collector should query `UsageEvents` for a defined time window.

Potentially useful events include:

* Activity resumed
* Activity paused
* Activity stopped
* Foreground service activity where relevant
* Screen interactive
* Screen non-interactive
* Device shutdown
* Device startup

Exact available event behavior varies by Android version and vendor.

The collector should convert raw events into normalized events:

```json
{
  "eventId": "sha256-derived-id",
  "applicationIdentifier": "com.google.android.youtube",
  "applicationDisplayName": "YouTube",
  "eventType": "ForegroundStarted",
  "timestampUtc": "2026-07-23T18:10:12Z",
  "source": "AndroidUsageEvents"
}
```

## 12.3 Session creation

Example raw sequence:

```text
18:10:12 YouTube resumed
18:32:40 YouTube paused
```

Normalized session:

```json
{
  "applicationIdentifier": "com.google.android.youtube",
  "startedUtc": "2026-07-23T18:10:12Z",
  "endedUtc": "2026-07-23T18:32:40Z",
  "foregroundSeconds": 1348,
  "completionReason": "ApplicationBackgrounded"
}
```

Sessions must account for:

* Missing close events
* Device shutdown
* Phone reboot
* Collector delay
* Duplicate events
* Overlapping events
* Midnight boundaries
* Time-zone changes
* Clock changes
* Split-screen and picture-in-picture
* System applications
* Launcher and lock screen
* Short accidental activations

## 12.4 Background work

Use WorkManager for:

* Periodic collection
* Batch aggregation
* Batch synchronization
* Retry processing
* Server-health reporting

Suggested workers:

```text
UsageCollectionWorker
UsageAggregationWorker
UsageSynchronizationWorker
AgentHealthWorker
RetentionCleanupWorker
```

WorkManager is intended for persistent scheduled work that the system can run when constraints are met, including after the application exits.

Suggested initial cadence:

```text
Collect:
Every 15–30 minutes where permitted

Synchronize:
Every 60 minutes

Health update:
Every 6 hours

Retention cleanup:
Once daily
```

The application must not promise exact execution times. Android may defer background work according to battery and operating-system policy.

## 12.5 Battery optimization

The app should work without demanding exemption from battery optimization wherever possible.

The diagnostics screen may report:

```text
Background execution:
Optimized by Android

Expected effect:
Synchronization can be delayed.
Collected usage remains available and will synchronize later.
```

Requesting unrestricted battery access should not be a default requirement.

## 12.6 Local Android storage

Store:

* Raw normalized events temporarily
* Aggregated sessions
* Upload batches
* Synchronization acknowledgements
* Collector checkpoint
* Permission history
* Diagnostic events

Do not depend on AsyncStorage for the telemetry queue.

Use a proper SQLite-backed store or Room on the native side.

---

# 13. iOS Native Implementation

## 13.1 Screen Time architecture

The iOS implementation requires:

* Family Controls authorization
* Device Activity framework
* Appropriate entitlements
* One or more app extensions
* App Group storage shared between the main app and extensions

Apple describes Device Activity as privacy-preserving activity monitoring that can operate through an app extension without requiring the main application to be open.

## 13.2 Required targets

Potential Xcode targets:

```text
GuardLAN Agent
GuardLAN Device Activity Monitor Extension
GuardLAN Device Activity Report Extension
```

Additional shield extensions are not required unless GuardLAN later introduces application blocking.

## 13.3 Authorization

The application must:

1. Explain the Screen Time integration.
2. Request Family Controls authorization.
3. Store the authorization state.
4. Provide clear recovery instructions if authorization is denied.
5. Report the effective capability set to GuardLAN.

## 13.4 Data limitations

The iOS implementation must be treated as a separate capability level.

It must not be designed around the assumption that iOS exposes Android-style unrestricted foreground events.

The iOS data may involve:

* Privacy-protected application tokens
* Device Activity reports
* Time aggregated by selected application or category
* Threshold-based activity notifications
* Extension-generated reports

The exact GuardLAN iOS functionality must be validated with a proof of concept before the shared API contract is finalized.

## 13.5 Export and synchronization

Where Apple’s APIs and granted entitlement allow exported activity data, the native Swift layer should normalize it into GuardLAN’s shared aggregate model. Apple documents activity-data access and export as requiring the appropriate Family Controls app-and-website-usage entitlement.

## 13.6 App Store considerations

The Family Controls entitlement must be requested for the application and applicable Screen Time extensions. Approval and distribution behavior must be tested early, not left until the final release.

For this reason, the iOS proof of concept is an early technical-risk phase rather than the last task in development.

---

# 14. Local Data Model

## Agent configuration

```typescript
export interface AgentConfiguration {
  agentId: string;
  deviceId: string;
  serverUrl: string;
  synchronizationIntervalMinutes: number;
  localRetentionDays: number;
  collectionEnabled: boolean;
  synchronizationEnabled: boolean;
}
```

## Application metadata

```typescript
export interface LocalApplication {
  applicationIdentifier: string;
  displayName: string | null;
  platform: 'Android' | 'iOS';
  category: string | null;
  firstObservedUtc: string;
  lastObservedUtc: string;
}
```

## Usage event

```typescript
export interface LocalUsageEvent {
  eventId: string;
  applicationIdentifier: string;
  eventType: UsageEventType;
  timestampUtc: string;
  source: UsageEventSource;
  processingState: ProcessingState;
}
```

## Usage session

```typescript
export interface LocalUsageSession {
  sessionId: string;
  applicationIdentifier: string;
  startedUtc: string;
  endedUtc: string;
  foregroundSeconds: number;
  completionReason: SessionCompletionReason;
  confidence: SessionConfidence;
  synchronizationState: SynchronizationState;
}
```

## Sync batch

```typescript
export interface LocalSyncBatch {
  batchId: string;
  createdUtc: string;
  attemptCount: number;
  nextAttemptUtc: string | null;
  state: SyncBatchState;
  payloadHash: string;
}
```

---

# 15. Synchronization Protocol

## 15.1 Batch synchronization

The mobile agent must send batches, not one request per event.

Endpoint:

```text
POST /api/mobile-agents/sync
```

Request:

```json
{
  "batchId": "019c...",
  "agentId": "019c...",
  "generatedUtc": "2026-07-23T19:00:00Z",
  "agentVersion": "1.0.0",
  "platform": "Android",
  "platformVersion": "16",
  "collectionWindow": {
    "fromUtc": "2026-07-23T18:00:00Z",
    "toUtc": "2026-07-23T19:00:00Z"
  },
  "applications": [],
  "usageSessions": [],
  "dailyAggregates": [],
  "agentHealth": {}
}
```

Response:

```json
{
  "batchId": "019c...",
  "status": "Accepted",
  "acceptedSessionIds": [],
  "rejectedRecords": [],
  "serverTimeUtc": "2026-07-23T19:00:03Z",
  "nextConfigurationVersion": 4
}
```

## 15.2 Idempotency

Every batch requires a unique ID.

The backend must support repeated delivery of the same batch without creating duplicates.

The agent may retry when:

* The connection fails
* The response is interrupted
* The server returns a retryable status
* Authentication is refreshed
* The device reconnects to the home network

## 15.3 Retry policy

Suggested retry sequence:

```text
First retry: 1 minute
Second retry: 5 minutes
Third retry: 15 minutes
Later retries: exponential delay with a maximum interval
```

Do not retry indefinitely at high frequency.

## 15.4 Compression

Usage batches should support HTTP compression when the payload becomes significant.

Raw packet content, screenshots, messages and audio must not be included.

---

# 16. GuardLAN Backend Additions

## Entities

```text
MobileAgent
MobileAgentCredential
MobileAgentCapability
MobileAgentPermissionState
MobileApplication
ApplicationUsageEvent
ApplicationUsageSession
ApplicationUsageAggregate
MobileAgentSyncBatch
MobileAgentHealthSnapshot
ActivityCorrelation
```

## Entity relationships

```text
MonitoredDevice
    |
    └── MobileAgent
            |
            ├── Capabilities
            ├── Permission states
            ├── Applications
            ├── Usage sessions
            ├── Sync batches
            └── Health snapshots
```

## Services

```text
MobileAgentRegistrationService
MobileAgentAuthenticationService
MobileAgentSyncService
ApplicationUsageNormalizationService
ApplicationUsageQueryService
AgentHealthService
ActivityCorrelationService
UsageRetentionService
```

## Repositories

```text
IMobileAgentRepository
IMobileApplicationRepository
IApplicationUsageSessionRepository
IApplicationUsageAggregateRepository
IMobileAgentSyncBatchRepository
IMobileAgentHealthRepository
```

## Endpoints

### Pairing

```text
POST /api/mobile-agent-pairings
POST /api/mobile-agent-pairings/complete
```

### Agent operations

```text
GET    /api/mobile-agents/me
POST   /api/mobile-agents/sync
POST   /api/mobile-agents/health
POST   /api/mobile-agents/token/refresh
DELETE /api/mobile-agents/me
```

### Dashboard queries

```text
GET /api/mobile-agents
GET /api/mobile-agents/{id}/details
GET /api/devices/{id}/application-usage
GET /api/application-usage/overview
GET /api/application-usage/timeline
```

### Administrative operations

```text
POST   /api/mobile-agents/{id}/revoke
DELETE /api/mobile-agents/{id}/collected-data
PATCH  /api/mobile-agents/{id}/configuration
```

---

# 17. Authentication and Security

## Pairing

Use a short-lived, single-use pairing code.

The pairing code must:

* Expire quickly
* Be stored hashed
* Be invalidated after use
* Be rate-limited
* Be bound to the GuardLAN installation

## Agent authentication

Each agent receives its own credential.

Do not use the dashboard user’s password on the phone.

Recommended:

* Short-lived access token
* Rotatable refresh token
* Device-specific token identity
* Token revocation
* Keychain or Keystore storage

Future hardening may add:

* Device-generated key pair
* Signed synchronization requests
* Certificate pinning
* Mutual TLS for advanced installations

## Transport

* HTTPS is required.
* Plain HTTP is development-only.
* TLS failures must not be silently ignored.
* Self-signed certificates require explicit local trust configuration.
* Tokens must never appear in logs.

## Data minimization

Do not collect:

* Message contents
* Contact lists
* Phone-call audio
* Microphone recordings
* Screenshots
* Keyboard input
* Passwords
* Clipboard contents
* Notification contents
* Exact GPS location
* Browser history content
* Full HTTPS request payloads

Collect only information required for application-usage monitoring.

---

# 18. Activity Correlation

The GuardLAN backend should correlate mobile-agent data with network observations.

## Example: YouTube

Mobile agent:

```text
YouTube foreground
18:10–18:42
32 minutes
```

Network data:

```text
YouTube-related destinations
18:11–18:39
2.1 GB received
Sustained QUIC traffic
```

GuardLAN result:

```text
Application:
YouTube

Foreground usage:
32 minutes

Estimated active video streaming:
28 minutes

Traffic:
2.1 GB

Confidence:
High
```

## Example: WhatsApp call

Mobile agent:

```text
WhatsApp foreground
20:03–20:46
43 minutes
```

Network data:

```text
Sustained bidirectional low-latency traffic
20:06–20:43
```

GuardLAN result:

```text
Application:
WhatsApp

Foreground usage:
43 minutes

Probable voice or video call:
37 minutes

Confidence:
Medium
```

The system must not claim certainty when the available information is inferred.

Classification values:

```text
Observed
Estimated
Probable
Unknown
```

Confidence values:

```text
Low
Medium
High
Verified
```

`Verified` should be reserved for data directly reported by an authoritative agent source, not network inference.

---

# 19. GuardLAN Dashboard Additions

## Application usage overview

Display:

* Total mobile usage today
* Total usage this week
* Most-used applications
* Devices with active agents
* Agents requiring attention
* Recent probable streaming sessions
* Recent probable call sessions

## Device usage page

One backend request should return:

```typescript
export interface DeviceApplicationUsagePageDto {
  device: DeviceSummaryDto;
  agent: MobileAgentSummaryDto;
  selectedPeriod: DateRangeDto;
  usageSummary: ApplicationUsageSummaryDto;
  applications: readonly ApplicationUsageItemDto[];
  timeline: readonly ApplicationUsageTimelineItemDto[];
  correlations: readonly ActivityCorrelationDto[];
  dataQuality: UsageDataQualityDto;
}
```

## Agent administration page

Display:

* Device
* Agent version
* Platform
* Last synchronized time
* Permission state
* Pending health warning
* Capability set
* Revocation status
* Data-retention policy

## Data-quality display

Every usage view should identify its source:

```text
Source:
Android usage access

Accuracy:
Direct application foreground statistics

Network correlation:
Available
```

Or:

```text
Source:
Network estimate only

Accuracy:
Application usage agent unavailable
```

---

# 20. Privacy Controls

The device owner must be able to:

* Pause collection
* Disable synchronization
* View locally collected data
* View the configured GuardLAN server
* Unpair the device
* Delete local data
* Request server-side deletion
* See the last synchronization time
* See exactly which data categories are enabled

GuardLAN administrators must be able to:

* Revoke an agent
* Delete collected usage
* Change retention
* Disable specific data categories
* See when consent or permissions were changed

---

# 21. Data Retention

Suggested defaults:

```text
Raw normalized events:
7 days

Application sessions:
90 days

Daily aggregates:
1 year

Agent health snapshots:
30 days

Failed synchronization diagnostics:
14 days
```

Retention must be configurable.

The backend should prefer storing sessions and aggregates long-term rather than permanent raw event history.

---

# 22. Observability

The mobile agent should record sanitized diagnostics for:

* Collector started
* Collector completed
* Permission missing
* Native API unavailable
* Synchronization started
* Synchronization succeeded
* Synchronization failed
* Token refreshed
* Database cleanup completed
* Unexpected native-module failure

Logs must not contain:

* Authentication tokens
* Complete synchronization payloads
* Sensitive application activity unless needed and explicitly sanitized
* Personal message or content data

The backend should track:

* Active agent count
* Last synchronization per agent
* Failed synchronization rate
* Rejected record rate
* Agent versions
* Permission health
* Collection delay
* Data gaps

---

# 23. Testing Strategy

## Shared React Native tests

Test:

* Pairing form
* Permission-state rendering
* Agent overview
* Synchronization states
* Error handling
* Privacy settings
* DTO validation
* API client
* Query caching
* Navigation

## Android native tests

Test:

* Permission-state detection
* Usage-event conversion
* Session creation
* Duplicate-event handling
* Missing-close-event handling
* Reboot boundaries
* Midnight boundaries
* WorkManager execution
* Retry behavior
* Local persistence
* Batch acknowledgement

## iOS native tests

Test:

* Authorization state
* App Group communication
* Extension execution
* Device Activity normalization
* Entitlement failure behavior
* Data export capability
* Background synchronization constraints

## Backend tests

Test:

* Pairing-code expiry
* Pairing-code reuse prevention
* Token rotation
* Batch idempotency
* Duplicate records
* Partial rejection
* Session validation
* Retention
* Agent revocation
* Correlation rules

## Accuracy tests

Compare collected results with:

* Android Digital Wellbeing
* Apple Screen Time
* Manual test sessions
* GuardLAN network observations

Example test:

```text
Open YouTube at 18:00
Keep foreground for 20 minutes
Pause video for 5 minutes
Background app
Synchronize agent
Compare:
- Foreground duration
- Network streaming duration
- GuardLAN displayed result
```

---

# 24. Delivery Phases

## Phase 1: Technical feasibility

Goal:

Prove that the required operating-system data can be collected.

Deliverables:

* Minimal React Native project
* Android native usage-access module
* Android permission-status screen
* Local display of recent usage
* Minimal iOS Family Controls proof of concept
* Entitlement investigation
* Written iOS capability report

Exit criteria:

* Android usage can be read reliably.
* Android foreground totals approximately match Digital Wellbeing.
* iOS capability boundaries are documented from a working prototype.
* No backend integration is required yet.

## Phase 2: Shared mobile foundation

Deliverables:

* Application navigation
* Shared design system
* Environment configuration
* Error handling
* Secure storage
* Local database
* API client
* Zod DTO validation
* Diagnostics infrastructure
* Development and production build variants

Exit criteria:

* Android and iOS applications build through the chosen pipeline.
* Custom native modules load successfully.
* Local configuration persists securely.
* Shared application architecture is stable.

## Phase 3: Android collection MVP

Deliverables:

* Usage permission workflow
* Usage event collector
* Session aggregator
* Application metadata resolver
* WorkManager collection job
* Local usage screen
* Local diagnostics
* Retention cleanup

Exit criteria:

* Collection survives app closure.
* Collection continues after reboot where permitted.
* Sessions remain locally available offline.
* Duplicate collection does not create duplicate sessions.
* Battery usage is acceptable.

## Phase 4: GuardLAN pairing and synchronization

Deliverables:

* Dashboard-generated pairing code
* Mobile pairing screen
* Device-specific authentication
* Sync-batch endpoint
* Local retry queue
* Idempotent backend ingestion
* Agent health reporting
* Manual synchronization

Exit criteria:

* A phone can pair without administrator credentials.
* Offline data synchronizes after reconnection.
* Repeated batch delivery does not duplicate data.
* Agent can be revoked.
* Tokens are stored securely.

## Phase 5: GuardLAN dashboard integration

Deliverables:

* Application usage overview
* Device usage page
* Daily and weekly totals
* Application timeline
* Agent administration
* Permission warnings
* Last-sync state
* Data-quality indicators

Exit criteria:

* Dashboard data comes from purpose-built DTOs.
* Each initial view uses one request.
* Missing or restricted data is clearly identified.
* Android results can be compared with the local agent view.

## Phase 6: Network correlation

Deliverables:

* Service-domain classification
* Application-to-network-session matching
* Probable streaming detection
* Probable voice/video call detection
* Confidence scoring
* Evidence view
* False-positive review workflow

Exit criteria:

* GuardLAN distinguishes direct usage data from inference.
* A probable call is not shown as a confirmed call.
* Correlation rules are testable.
* Users can inspect the evidence behind a classification.

## Phase 7: iOS implementation

Deliverables:

* Family Controls authorization
* Required Screen Time extensions
* App Group storage
* Device Activity data normalization
* iOS capability reporting
* iOS synchronization
* iOS-specific dashboard labels
* Distribution and entitlement validation

Exit criteria:

* Supported iOS data synchronizes reliably.
* Unsupported Android-equivalent fields remain optional.
* Entitlements are valid for the intended distribution model.
* Dashboard does not misrepresent iOS accuracy.

## Phase 8: Hardening and release preparation

Deliverables:

* Threat model
* Token rotation
* Rate limiting
* Data-retention jobs
* Database encryption review
* Certificate strategy
* Sanitized diagnostics export
* Upgrade and migration strategy
* End-to-end tests
* Privacy documentation
* Installation documentation
* Android release build
* iOS release candidate where permitted

Exit criteria:

* No hidden collection behavior exists.
* Collection can be paused and removed.
* Server-side deletion works.
* Credentials are revocable.
* Upgrade migrations preserve pending data.
* Monitoring limitations are documented.

---

# 25. MVP Definition

The first usable release should be Android-focused.

MVP includes:

* React Native application
* Android usage-access permission
* Kotlin usage native module
* Foreground usage totals
* Application sessions
* Local SQLite queue
* WorkManager collection and synchronization
* GuardLAN pairing
* Secure device authentication
* Batch synchronization
* GuardLAN application-usage dashboard
* Agent health reporting
* Pause and unpair controls

MVP does not include:

* Confirmed WhatsApp-call detection
* iOS parity
* Application blocking
* Message monitoring
* Call recording
* Screenshots
* Content inspection
* Public cloud hosting
* Machine-learning detection

---

# 26. Recommended Implementation Strategy

Use React Native for the product, but treat the collectors as native subsystems.

```text
Shared once:
- UI
- Navigation
- API
- Pairing
- Authentication
- Settings
- Diagnostics
- Local usage presentation
- Synchronization coordination

Implemented per platform:
- Permission handling
- Usage collection
- Background scheduling
- Screen Time extensions
- Native persistence where workers require it
```

Do not attempt to force the complete collector into JavaScript.

The JavaScript runtime may not be alive when the operating system permits collection or synchronization. Native code must be capable of performing critical background work independently.

---

# 27. Coding-Agent Rules

When implementing MDAC:

1. Keep shared behavior in TypeScript where practical.
2. Keep operating-system access inside typed Turbo Native Modules.
3. Do not use legacy untyped native bridges for new modules.
4. Do not rely on Expo Go.
5. Use a custom development client.
6. Do not rely on a permanent JavaScript background process.
7. Use WorkManager for Android persistent work.
8. Use Apple-supported Screen Time frameworks and extensions on iOS.
9. Never bypass operating-system permission flows.
10. Never disguise the application or hide collection.
11. Store credentials only in secure platform storage.
12. Store telemetry in a proper local database.
13. Synchronize records in idempotent batches.
14. Do not send one request per usage event.
15. Do not collect message, call or screen contents.
16. Keep platform capability differences explicit.
17. Do not claim Android and iOS feature parity without proof.
18. Normalize timestamps to UTC.
19. Preserve source and confidence metadata.
20. Keep direct observations separate from inferred activity.
21. Make collection pausable.
22. Make unpairing and deletion available.
23. Record sanitized diagnostic information.
24. Validate API responses at runtime.
25. Add tests for missing, duplicate and delayed events.
26. Verify results against operating-system usage dashboards.
27. Treat iOS entitlement validation as an early risk.
28. Document every new collected data category.
29. Require explicit approval before adding a sensitive permission.
30. Keep battery consumption measurable and controlled.

---

# 28. Definition of Done

The mobile-agent feature is complete when:

* Android and iOS share one React Native application shell.
* Native modules expose typed platform functionality.
* Android usage collection works while the React Native UI is closed.
* iOS uses only approved Screen Time APIs and entitlements.
* Every device explicitly authorizes collection.
* Application usage is stored locally before transmission.
* Synchronization is batched and idempotent.
* Offline collection is supported.
* Authentication credentials are securely stored.
* Agents can be revoked.
* Users can pause collection and unpair.
* Server-side collected data can be deleted.
* Dashboard values identify their source and confidence.
* Network-inferred activity is never presented as certain.
* Permission loss is visible in the app and dashboard.
* Retention is configurable.
* Tests cover collection, synchronization and failure recovery.
* Monitoring limitations are documented.
