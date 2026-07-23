# Angular Frontend Architecture and Development Guidelines

## 1. Purpose

This document defines the required frontend architecture for the GuardLAN network-monitoring application.

Every new page, component, API integration and user interaction must follow the same architectural rules.

The frontend uses:

* Angular with standalone components
* Angular Material with Material 3
* Signals for synchronous UI state
* RxJS for asynchronous event streams
* Typed DTOs for backend communication
* Feature-based architecture
* Lazy-loaded routes
* Reusable generic presentation components
* Reactive forms
* Centralized styling and design tokens
* Strict TypeScript configuration

The frontend is primarily responsible for:

* Displaying data
* Collecting user input
* Managing temporary UI state
* Triggering backend operations
* Presenting loading, empty and error states
* Navigating between views

The frontend is not responsible for implementing business logic that belongs to the backend.

---

# 2. Core Architectural Principles

## 2.1 One HTTP Request per User Action or View

A page load, user click or explicit refresh should normally trigger no more than one HTTP request.

Examples:

```text
Opening the dashboard
    -> GET /api/dashboard

Opening a device detail page
    -> GET /api/devices/{id}/details

Changing the trusted state
    -> PATCH /api/devices/{id}/trust

Refreshing the alerts page
    -> GET /api/alerts
```

The frontend must not assemble one screen by calling many individual endpoints when the backend can provide a purpose-built response DTO.

Incorrect:

```text
Open device detail page

1. GET /api/devices/{id}
2. GET /api/devices/{id}/connections
3. GET /api/devices/{id}/dns-queries
4. GET /api/devices/{id}/alerts
5. GET /api/devices/{id}/traffic-summary
```

Correct:

```text
Open device detail page

1. GET /api/devices/{id}/details
```

The response should contain the complete initial view model:

```typescript
export interface DeviceDetailsDto {
  device: DeviceDto;
  connectionSummary: ConnectionSummaryDto;
  dnsSummary: DnsSummaryDto;
  recentAlerts: AlertListItemDto[];
  trafficSummary: TrafficSummaryDto;
}
```

This rule reduces:

* Network overhead
* Loading-state complexity
* Partial rendering
* Race conditions
* Duplicate error handling
* Backend round trips
* Frontend orchestration logic

Exceptions require a concrete technical reason, such as:

* Independent lazy-loaded tabs
* Infinite scrolling
* Streaming data
* Long-running exports
* Explicit manual refresh of one section
* Real-time updates through SignalR
* Data too large for the initial response

The agent must not introduce multiple requests merely because several backend endpoints already exist.

---

# 3. Responsibility Boundaries

## 3.1 Backend Responsibilities

The backend must handle:

* Business rules
* Authorization decisions
* Validation requiring persisted data
* Data aggregation
* Filtering
* Sorting
* Pagination
* Calculations
* Statistics
* Classification
* Alert severity determination
* Device status determination
* Domain normalization
* Protocol interpretation
* Permission checks
* Data transformation requiring domain knowledge

Example:

The frontend must not determine whether a device is suspicious.

Incorrect:

```typescript
const isSuspicious =
  device.isUnknown &&
  device.outboundConnectionCount > 100 &&
  device.uniqueDomains > 50;
```

Correct:

```typescript
export interface DeviceDto {
  id: string;
  displayName: string;
  riskLevel: DeviceRiskLevel;
  riskReason: string | null;
}
```

The backend calculates `riskLevel` and provides the result.

## 3.2 Frontend Responsibilities

The frontend may handle:

* Whether a dialog is open
* Current selected tab
* Expanded table row
* Local search-field value
* Form input state
* Loading indicators
* Presentation formatting
* Temporary sorting before server-side sorting is required
* Responsive layout behavior
* User-interface preferences
* Navigation state

The frontend should display backend results rather than reinterpret them.

---

# 4. Required Data Flow

The standard data flow is:

```text
User Interaction
    |
Smart Page Component
    |
Feature Facade or Store
    |
Typed API Client
    |
Backend Endpoint
    |
Response DTO
    |
Feature State
    |
Presentational Components
```

A response should flow through the application as immutable data:

```text
HTTP response
    |
DTO
    |
Signal or Observable state
    |
Computed presentation state
    |
Template
```

Components must not manually mutate DTO objects received from the backend.

---

# 5. Project Structure

The preferred structure is:

```text
src/
  app/
    core/
    layout/
    shared/
    features/

    app.config.ts
    app.routes.ts
    app.component.ts

  styles/
    _tokens.scss
    _theme.scss
    _typography.scss
    _layout.scss
    _utilities.scss
    styles.scss

  environments/
```

---

# 6. Core Layer

The `core` folder contains application-wide infrastructure that should normally have only one instance.

```text
core/
  api/
    api-client.ts
    api-error.ts
    api-result.ts

  authentication/
    auth.service.ts
    auth.interceptor.ts
    auth.guard.ts

  configuration/
    app-config.ts
    app-config.service.ts

  error-handling/
    global-error-handler.ts
    problem-details.ts

  http/
    loading.interceptor.ts
    correlation-id.interceptor.ts
    error.interceptor.ts

  notifications/
    notification.service.ts

  realtime/
    realtime.service.ts

  routing/
    pending-changes.guard.ts
```

Rules:

* `core` must not contain feature-specific code.
* Feature components must not be stored in `core`.
* Feature DTOs must not be stored in `core`.
* `core` services should be application-wide infrastructure.
* Global providers belong in `app.config.ts`.
* Avoid generic dumping grounds such as `core/helpers`.

---

# 7. Shared Layer

The `shared` folder contains reusable, domain-independent UI building blocks.

```text
shared/
  components/
    page-header/
    data-table/
    status-chip/
    empty-state/
    loading-state/
    error-state/
    confirmation-dialog/
    search-field/
    metric-card/
    section-card/

  directives/
    autofocus.directive.ts
    stop-propagation.directive.ts

  pipes/
    bytes.pipe.ts
    duration.pipe.ts
    relative-time.pipe.ts

  models/
    select-option.ts
    table-column.ts

  utilities/
    track-by.utilities.ts
```

Shared components must be:

* Reusable across multiple features
* Independent from feature services
* Independent from concrete API endpoints
* Driven through inputs
* Communicating through outputs
* Focused on presentation
* Consistent with the Material 3 style guide

A shared component must not import a feature facade.

Incorrect:

```typescript
@Component(...)
export class DataTableComponent {
  private readonly deviceFacade = inject(DeviceFacade);
}
```

Correct:

```typescript
@Component(...)
export class DataTableComponent<T> {
  readonly rows = input.required<readonly T[]>();
  readonly columns = input.required<readonly TableColumn<T>[]>();

  readonly rowSelected = output<T>();
}
```

---

# 8. Feature Structure

Each domain feature is self-contained.

Example:

```text
features/
  devices/
    data-access/
      device.api.ts
      device.facade.ts
      device.store.ts

    models/
      device.dto.ts
      device-details.dto.ts
      device-query.dto.ts
      update-device.dto.ts

    pages/
      device-list-page/
        device-list-page.component.ts
        device-list-page.component.html
        device-list-page.component.scss

      device-details-page/
        device-details-page.component.ts
        device-details-page.component.html
        device-details-page.component.scss

    components/
      device-table/
      device-summary/
      device-status-chip/
      device-details-header/

    dialogs/
      edit-device-dialog/

    devices.routes.ts
```

Other features should follow the same structure:

```text
features/
  dashboard/
  devices/
  connections/
  dns-queries/
  alerts/
  settings/
```

Feature boundaries must be respected.

A component in `devices` must not directly import private implementation code from `alerts`.

Cross-feature data should be:

* Returned from a backend view endpoint
* Exposed through a public feature API
* Moved into `shared` only when truly domain-independent

---

# 9. Standalone Components

All new components, directives and pipes must be standalone.

Example:

```typescript
@Component({
  selector: 'app-device-status-chip',
  standalone: true,
  imports: [MatChipsModule],
  templateUrl: './device-status-chip.component.html',
  styleUrl: './device-status-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeviceStatusChipComponent {
  readonly isOnline = input.required<boolean>();
}
```

Rules:

* Do not create new `NgModule` files.
* Import only what the component needs.
* Lazy-load feature routes.
* Keep component dependency lists explicit.
* Use `ChangeDetectionStrategy.OnPush`.
* Prefer signal-based `input()` and `output()` APIs for new components.
* Avoid large components importing every Material module.

---

# 10. Routing

Features must be lazy-loaded.

Root routes:

```typescript
export const appRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./features/dashboard/dashboard.routes')
        .then(routes => routes.DASHBOARD_ROUTES)
  },
  {
    path: 'devices',
    loadChildren: () =>
      import('./features/devices/devices.routes')
        .then(routes => routes.DEVICE_ROUTES)
  },
  {
    path: 'alerts',
    loadChildren: () =>
      import('./features/alerts/alerts.routes')
        .then(routes => routes.ALERT_ROUTES)
  },
  {
    path: '**',
    loadComponent: () =>
      import('./shared/components/not-found/not-found.component')
        .then(component => component.NotFoundComponent)
  }
];
```

Feature routes:

```typescript
export const DEVICE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/device-list-page/device-list-page.component')
        .then(component => component.DeviceListPageComponent)
  },
  {
    path: ':deviceId',
    loadComponent: () =>
      import('./pages/device-details-page/device-details-page.component')
        .then(component => component.DeviceDetailsPageComponent)
  }
];
```

Rules:

* Route components represent pages.
* Reusable components must not depend directly on route parameters.
* Parse route parameters in the page component or route resolver.
* Use route-level providers for feature-scoped facades where appropriate.
* Do not preload every heavy feature without a measured benefit.
* URL state should be used for shareable filters, searches and pagination.

Example:

```text
/devices?page=2&pageSize=25&status=online&search=samsung
```

---

# 11. DTO Rules

The frontend must use DTOs matching the backend API contract.

Example:

```typescript
export interface DeviceDto {
  id: string;
  ipAddress: string;
  macAddress: string;
  hostname: string | null;
  displayName: string | null;
  vendor: string | null;
  deviceType: DeviceType;
  isTrusted: boolean;
  isOnline: boolean;
  firstSeenUtc: string;
  lastSeenUtc: string;
}
```

Request DTO:

```typescript
export interface UpdateDeviceDto {
  displayName: string | null;
  deviceType: DeviceType;
  isTrusted: boolean;
}
```

View-specific response DTO:

```typescript
export interface DeviceListPageDto {
  devices: PagedResultDto<DeviceListItemDto>;
  summary: DeviceListSummaryDto;
  availableFilters: DeviceFilterOptionsDto;
}
```

Rules:

* Do not use backend entities as frontend models.
* Do not use `any`.
* Do not use untyped object maps for known API data.
* Separate request DTOs from response DTOs.
* Do not add frontend-only properties to API DTOs.
* Create a separate view model when local presentation state is required.
* Preserve backend nullability.
* Date values received as JSON remain strings at the API boundary.
* Convert date strings only where required for display or calculation.
* Enums must be represented consistently across backend and frontend.
* API DTOs belong in the feature `models` folder.
* One model should have one clear purpose.

Incorrect:

```typescript
export interface Device {
  id: any;
  data: any;
  selected: boolean;
  expanded: boolean;
  rawEntity: unknown;
}
```

Correct:

```typescript
export interface DeviceListItemDto {
  id: string;
  displayName: string;
  ipAddress: string;
  status: DeviceStatus;
}

export interface DeviceListItemViewModel {
  device: DeviceListItemDto;
  selected: boolean;
}
```

---

# 12. API Client Rules

Every feature should have one typed API client.

Example:

```typescript
@Injectable({
  providedIn: 'root'
})
export class DeviceApi {
  private readonly http = inject(HttpClient);

  getDeviceList(
    query: DeviceQueryDto
  ): Observable<DeviceListPageDto> {
    return this.http.get<DeviceListPageDto>(
      '/api/devices',
      {
        params: this.createQueryParams(query)
      }
    );
  }

  getDeviceDetails(
    deviceId: string
  ): Observable<DeviceDetailsPageDto> {
    return this.http.get<DeviceDetailsPageDto>(
      `/api/devices/${deviceId}/details`
    );
  }

  updateDevice(
    deviceId: string,
    dto: UpdateDeviceDto
  ): Observable<DeviceDto> {
    return this.http.put<DeviceDto>(
      `/api/devices/${deviceId}`,
      dto
    );
  }

  setDeviceTrust(
    deviceId: string,
    dto: SetDeviceTrustDto
  ): Observable<DeviceDto> {
    return this.http.patch<DeviceDto>(
      `/api/devices/${deviceId}/trust`,
      dto
    );
  }

  private createQueryParams(query: DeviceQueryDto): HttpParams {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.isOnline !== null) {
      params = params.set('isOnline', query.isOnline);
    }

    return params;
  }
}
```

Rules:

* Components must not use `HttpClient` directly.
* API clients must return typed `Observable<T>` values.
* API clients must not subscribe internally.
* API clients must not contain UI state.
* API clients must not display notifications.
* API clients must not contain business decisions.
* URL construction belongs in API clients.
* Request parameter construction belongs in API clients.
* Errors should be centrally normalized.
* Do not repeat the base API URL throughout the application.

---

# 13. Reactive Programming

Reactive code is the default.

The application should describe:

* What the state depends on
* How derived values are calculated
* How asynchronous work reacts to state changes

It should not manually coordinate every state transition through imperative assignments.

## Preferred

```typescript
readonly searchTerm = signal('');
readonly selectedStatus = signal<DeviceStatus | null>(null);

readonly query = computed<DeviceQueryDto>(() => ({
  search: this.searchTerm().trim() || null,
  status: this.selectedStatus(),
  page: 1,
  pageSize: 25
}));
```

## Avoid

```typescript
searchTerm = '';
selectedStatus: DeviceStatus | null = null;
query!: DeviceQueryDto;

updateQuery(): void {
  this.query = {
    search: this.searchTerm.trim(),
    status: this.selectedStatus,
    page: 1,
    pageSize: 25
  };
}
```

Use:

* `signal()` for writable synchronous state
* `computed()` for derived state
* `linkedSignal()` for writable state derived from another source
* RxJS for HTTP, websocket and event streams
* `toSignal()` when an observable must be consumed as signal state
* `toObservable()` when signal state must enter an RxJS pipeline
* `effect()` only for genuine side effects

---

# 14. Signals Guidelines

## Use Signals For

* Local component state
* Selected row
* Current tab
* Loading state
* Error state
* Dialog state
* Feature state
* Derived presentation values
* State shared within one feature
* State read by templates

Example:

```typescript
@Injectable()
export class DeviceStore {
  private readonly _devices =
    signal<readonly DeviceListItemDto[]>([]);

  private readonly _loading = signal(false);
  private readonly _error = signal<ApiError | null>(null);

  readonly devices = this._devices.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly onlineDevices = computed(() =>
    this._devices().filter(device => device.isOnline)
  );

  setDevices(devices: readonly DeviceListItemDto[]): void {
    this._devices.set(devices);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setError(error: ApiError | null): void {
    this._error.set(error);
  }
}
```

## Do Not Use Signals For Everything

RxJS remains appropriate for:

* HTTP request pipelines
* Debounced searches
* Cancellation with `switchMap`
* WebSocket streams
* SignalR events
* Complex event composition
* Retry logic
* Time-based operators
* Combining asynchronous sources

Example:

```typescript
private readonly refreshRequested = new Subject<void>();

readonly deviceDetails = toSignal(
  this.refreshRequested.pipe(
    startWith(undefined),
    switchMap(() =>
      this.deviceApi.getDeviceDetails(this.deviceId())
    )
  ),
  {
    initialValue: null
  }
);
```

## Computed Values

Derived state must use `computed()` rather than being copied into another writable signal.

Incorrect:

```typescript
readonly devices = signal<DeviceDto[]>([]);
readonly onlineDeviceCount = signal(0);

updateDevices(devices: DeviceDto[]): void {
  this.devices.set(devices);
  this.onlineDeviceCount.set(
    devices.filter(device => device.isOnline).length
  );
}
```

Correct:

```typescript
readonly devices = signal<readonly DeviceDto[]>([]);

readonly onlineDeviceCount = computed(() =>
  this.devices().filter(device => device.isOnline).length
);
```

## Effects

Use `effect()` only for side effects such as:

* Synchronizing state to local storage
* Updating non-reactive third-party APIs
* Logging meaningful state changes
* Triggering browser APIs
* Integrating with imperative chart libraries

Do not use an effect to copy one signal into another.

Incorrect:

```typescript
effect(() => {
  this.filteredDevices.set(
    this.devices().filter(device =>
      device.displayName.includes(this.search())
    )
  );
});
```

Correct:

```typescript
readonly filteredDevices = computed(() => {
  const search = this.search().trim().toLowerCase();

  if (!search) {
    return this.devices();
  }

  return this.devices().filter(device =>
    device.displayName
      .toLowerCase()
      .includes(search)
  );
});
```

---

# 15. Facade Pattern

Page components should communicate with a feature facade rather than coordinating API calls directly.

Example:

```typescript
@Injectable()
export class DeviceDetailsFacade {
  private readonly deviceApi = inject(DeviceApi);
  private readonly notifications = inject(NotificationService);

  private readonly _state =
    signal<DeviceDetailsState>({
      data: null,
      loading: false,
      error: null
    });

  readonly state = this._state.asReadonly();

  readonly data = computed(() => this._state().data);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);

  load(deviceId: string): void {
    this._state.update(state => ({
      ...state,
      loading: true,
      error: null
    }));

    this.deviceApi
      .getDeviceDetails(deviceId)
      .pipe(
        take(1),
        finalize(() => {
          this._state.update(state => ({
            ...state,
            loading: false
          }));
        })
      )
      .subscribe({
        next: data => {
          this._state.update(state => ({
            ...state,
            data
          }));
        },
        error: error => {
          this._state.update(state => ({
            ...state,
            error
          }));
        }
      });
  }
}
```

The facade may:

* Trigger API requests
* Coordinate feature state
* Expose readonly signals
* Map API errors to feature state
* Coordinate dialogs and refreshes
* Update state after successful writes

The facade must not:

* Contain HTML-specific behavior
* Access DOM elements
* Format values that belong in pipes
* Duplicate backend business logic
* Return mutable internal signals
* Become a global service containing unrelated features

---

# 16. Page Components

Page components are smart components.

They may:

* Read route parameters
* Inject the feature facade
* Trigger initial loading
* Handle page-level user actions
* Open dialogs
* Navigate
* Pass data into presentational components

Example:

```typescript
@Component({
  selector: 'app-device-details-page',
  standalone: true,
  imports: [
    DeviceDetailsHeaderComponent,
    DeviceSummaryComponent,
    LoadingStateComponent,
    ErrorStateComponent
  ],
  providers: [DeviceDetailsFacade],
  templateUrl: './device-details-page.component.html',
  styleUrl: './device-details-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeviceDetailsPageComponent {
  private readonly route = inject(ActivatedRoute);
  readonly facade = inject(DeviceDetailsFacade);

  readonly deviceId = computed(() =>
    this.route.snapshot.paramMap.get('deviceId')
  );

  constructor() {
    const deviceId = this.deviceId();

    if (deviceId) {
      this.facade.load(deviceId);
    }
  }
}
```

Page components should remain thin.

The page should not manually construct complex domain models.

---

# 17. Presentational Components

Presentational components receive data and emit user intentions.

Example:

```typescript
@Component({
  selector: 'app-device-details-header',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    DeviceStatusChipComponent
  ],
  templateUrl: './device-details-header.component.html',
  styleUrl: './device-details-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeviceDetailsHeaderComponent {
  readonly device = input.required<DeviceDto>();

  readonly editRequested = output<void>();
  readonly trustChangeRequested = output<boolean>();
}
```

Rules:

* Inputs must be readonly.
* Outputs describe user intentions.
* Components must not know backend URLs.
* Components must not inject feature API clients.
* Components must not perform data loading.
* Components must not mutate input objects.
* Components must support loading and missing optional data safely.
* Components should be reusable within the feature.

Preferred output names:

```text
editRequested
deleteRequested
refreshRequested
rowSelected
filterChanged
pageChanged
trustChangeRequested
```

Avoid:

```text
clicked
doAction
handle
changed
event
```

---

# 18. Component Size and Responsibilities

A component should have one clear responsibility.

Extract a component when:

* A section has independent presentation logic
* A section is repeated
* A section has a meaningful input/output contract
* The parent template becomes difficult to understand
* A component can be tested independently
* The component represents a reusable design pattern

Do not extract components solely to reduce line count.

Avoid generic components that attempt to support every possible case through dozens of inputs.

Incorrect:

```html
<app-universal-component
  [type]="17"
  [mode]="'device'"
  [showHeader]="true"
  [showFooter]="false"
  [enableSpecialMode]="true"
  [customVariant]="'network'">
</app-universal-component>
```

Prefer small, intentional components with clear contracts.

---

# 19. Generic Components

The application should provide a controlled set of reusable components.

Recommended generic components:

```text
AppPageHeader
AppSectionCard
AppMetricCard
AppDataTable
AppSearchField
AppStatusChip
AppEmptyState
AppLoadingState
AppErrorState
AppConfirmationDialog
AppFilterBar
AppPagination
AppDateRangePicker
```

Example generic table contract:

```typescript
export interface TableColumn<T> {
  key: string;
  header: string;
  cell: (row: T) => string | number | null;
  sortable?: boolean;
  width?: string;
}
```

```typescript
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [
    MatTableModule,
    MatSortModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DataTableComponent<T> {
  readonly rows = input.required<readonly T[]>();
  readonly columns = input.required<readonly TableColumn<T>[]>();
  readonly loading = input(false);
  readonly rowClickable = input(false);

  readonly rowSelected = output<T>();
  readonly sortChanged = output<Sort>();
}
```

Generic components must not:

* Know feature DTOs
* Contain backend logic
* Perform HTTP requests
* Contain domain-specific conditions
* Hard-code device, alert or DNS terminology
* Become excessively configurable

---

# 20. Material 3 Style Guide

Angular Material is the foundation of the UI.

Material components should be used before creating custom replacements.

Preferred Angular Material components:

```text
MatToolbar
MatSidenav
MatButton
MatIconButton
MatCard
MatTable
MatSort
MatPaginator
MatFormField
MatInput
MatSelect
MatCheckbox
MatSlideToggle
MatDialog
MatSnackBar
MatMenu
MatTabs
MatTooltip
MatProgressSpinner
MatProgressBar
MatChips
MatBadge
```

Do not create custom buttons, dialogs, fields, tables or menus when Angular Material already provides an appropriate accessible component.

---

# 21. Centralized Material 3 Theme

The application must define one centralized Material 3 theme.

Example structure:

```text
styles/
  _tokens.scss
  _theme.scss
  _typography.scss
  _layout.scss
  _utilities.scss
  styles.scss
```

Theme example:

```scss
@use '@angular/material' as mat;

html {
  @include mat.theme((
    color: (
      primary: mat.$azure-palette,
      tertiary: mat.$blue-palette
    ),
    typography: Roboto,
    density: 0
  ));
}
```

The exact theme API must follow the installed Angular Material version.

Rules:

* Theme configuration belongs in one central file.
* Do not define arbitrary colors inside feature components.
* Do not override internal Material DOM selectors without necessity.
* Do not use deprecated Material 2 theming APIs for new code.
* Typography must be centralized.
* Density must be chosen consistently.
* Light and dark modes must use the same design-token structure.
* Custom components should consume theme tokens rather than duplicate values.

---

# 22. Design Tokens

Reusable design values must be defined as tokens.

Example:

```scss
:root {
  --app-spacing-1: 0.25rem;
  --app-spacing-2: 0.5rem;
  --app-spacing-3: 0.75rem;
  --app-spacing-4: 1rem;
  --app-spacing-6: 1.5rem;
  --app-spacing-8: 2rem;

  --app-radius-small: 0.5rem;
  --app-radius-medium: 0.75rem;
  --app-radius-large: 1rem;

  --app-page-max-width: 1600px;
  --app-content-gap: 1.5rem;
}
```

Use tokens for:

* Spacing
* Border radii
* Layout widths
* Header heights
* Navigation widths
* Chart heights
* Semantic status colors
* Responsive breakpoints

Do not scatter values such as these across components:

```scss
padding: 17px;
border-radius: 13px;
gap: 22px;
```

Use the defined spacing system.

---

# 23. Unified Visual Language

Every feature must follow the same patterns.

## Page Structure

```text
Page header
    |
Optional actions
    |
Optional summary metrics
    |
Filter bar
    |
Primary content
    |
Pagination or secondary actions
```

## Card Structure

```text
Card title
Optional subtitle
Optional action
Content
Optional footer
```

## Status Presentation

Use consistent semantic states:

```text
Positive
Informational
Warning
Critical
Neutral
Offline
```

Examples:

```text
Online       -> Positive
Trusted      -> Positive
Unknown      -> Warning
Suspicious   -> Critical
Offline      -> Neutral
Blocked      -> Critical or warning
Resolved     -> Neutral
```

The same state must not use different colors or labels across pages.

---

# 24. Styling Rules

Component styles must remain local whenever possible.

Use global styles only for:

* Material theme
* Typography
* Page layout primitives
* Design tokens
* CSS reset
* Reusable utility classes
* Accessibility helpers

Rules:

* Use SCSS consistently.
* Prefer CSS Grid and Flexbox.
* Avoid deeply nested selectors.
* Avoid `!important`.
* Avoid global feature-specific classes.
* Avoid styling Material internal implementation classes.
* Use host selectors for component-level layout.
* Keep responsive behavior near the component it affects.
* Use logical CSS properties where appropriate.
* Keep templates semantic.

Example:

```scss
:host {
  display: block;
}

.device-grid {
  display: grid;
  grid-template-columns:
    repeat(auto-fit, minmax(18rem, 1fr));
  gap: var(--app-spacing-4);
}
```

---

# 25. Responsive Design

All pages must support:

* Desktop
* Tablet
* Mobile

Network-monitoring pages are desktop-oriented, but the application must remain usable on smaller screens.

Guidelines:

* Tables may switch to cards on narrow screens.
* Side navigation should collapse.
* Page actions should wrap.
* Dialogs should respect viewport width.
* Touch targets must remain usable.
* Horizontal scrolling should be limited to data tables where unavoidable.
* Critical information must not depend on hover.
* Charts must resize with their container.

Use Angular CDK layout utilities when breakpoint behavior requires TypeScript.

Prefer CSS media or container queries when behavior is purely visual.

---

# 26. HTTP Loading Behavior

Every request must expose a loading state.

A view must support:

```text
Initial loading
Loaded with data
Loaded without data
Recoverable error
Refreshing existing data
Saving
Deleting
```

Do not display an empty state while data is still loading.

Incorrect:

```html
@if (!devices().length) {
  <app-empty-state />
}
```

Correct:

```html
@if (loading()) {
  <app-loading-state />
} @else if (error()) {
  <app-error-state
    [error]="error()"
    (retryRequested)="reload()" />
} @else if (devices().length === 0) {
  <app-empty-state />
} @else {
  <app-device-table [devices]="devices()" />
}
```

When refreshing existing data:

* Keep current data visible where possible.
* Show a subtle progress indicator.
* Avoid clearing the complete page.
* Prevent duplicate requests.

---

# 27. Preventing Duplicate HTTP Requests

The agent must check for duplicate request triggers.

Common causes:

* Calling a load method in multiple lifecycle hooks
* Multiple subscriptions to a cold HTTP observable
* Calling a method directly from a template
* Effects that rerun because of accidental signal dependencies
* Parent and child components loading the same data
* Route resolver and page component both loading
* Recreating an observable for every change-detection pass

Incorrect:

```html
<div>{{ loadDevice() | async }}</div>
```

Incorrect:

```typescript
ngOnInit(): void {
  this.load();
}

ngAfterViewInit(): void {
  this.load();
}
```

When sharing an observable request result, use an appropriate sharing strategy:

```typescript
readonly deviceDetails$ = this.deviceApi
  .getDeviceDetails(this.deviceId)
  .pipe(
    shareReplay({
      bufferSize: 1,
      refCount: true
    })
  );
```

When using a facade, the facade should own the request lifecycle.

---

# 28. User Action Requests

A button click must trigger one clear operation.

Example:

```typescript
setTrusted(isTrusted: boolean): void {
  if (this.saving()) {
    return;
  }

  this.saving.set(true);

  this.deviceApi
    .setDeviceTrust(
      this.deviceId(),
      { isTrusted }
    )
    .pipe(
      take(1),
      finalize(() => this.saving.set(false))
    )
    .subscribe({
      next: updatedDevice => {
        this.updateDeviceState(updatedDevice);
      },
      error: error => {
        this.handleSaveError(error);
      }
    });
}
```

Rules:

* Disable actions while the request is running.
* Prevent repeated clicks.
* Update state from the backend response.
* Do not perform an automatic second GET when the mutation response already returns the updated DTO.
* Reload only when the backend mutation response cannot provide sufficient state.
* Display a clear success or failure result.
* Destructive actions require confirmation.

Incorrect:

```text
PATCH device
GET device
GET device list
GET dashboard counters
```

Correct:

```text
PATCH device

Response contains:
- updated device
- updated relevant summary values
```

Alternatively, update cached state from the returned DTO.

---

# 29. Forms

Forms must be reactive.

Use:

* Angular Reactive Forms
* Signal Forms when adopted as a project-wide decision
* Typed form controls
* Centralized validation messages
* Backend validation errors mapped to form state

Example:

```typescript
readonly form = new FormGroup({
  displayName: new FormControl<string | null>(
    null,
    {
      validators: [
        Validators.maxLength(100)
      ]
    }
  ),
  deviceType: new FormControl<DeviceType>(
    DeviceType.Unknown,
    {
      nonNullable: true,
      validators: [
        Validators.required
      ]
    }
  ),
  isTrusted: new FormControl<boolean>(
    false,
    {
      nonNullable: true
    }
  )
});
```

Rules:

* Do not use template-driven forms for application features.
* Do not duplicate backend business validation.
* Perform basic client validation for user feedback.
* The backend remains authoritative.
* Disable submission while saving.
* Do not send the form when invalid.
* Map form values explicitly to request DTOs.
* Do not submit the complete response DTO.
* Preserve unsaved-change protection where relevant.

Example:

```typescript
private createUpdateDto(): UpdateDeviceDto {
  const value = this.form.getRawValue();

  return {
    displayName: value.displayName?.trim() || null,
    deviceType: value.deviceType,
    isTrusted: value.isTrusted
  };
}
```

---

# 30. Error Handling

API errors must use a unified error model.

Example:

```typescript
export interface ApiProblemDetails {
  type: string | null;
  title: string;
  status: number;
  detail: string | null;
  traceId: string | null;
  errors?: Record<string, readonly string[]>;
}
```

The HTTP error interceptor should:

* Parse backend `ProblemDetails`
* Preserve status codes
* Preserve trace IDs
* Normalize connection failures
* Avoid exposing raw internal errors
* Re-throw a typed application error

Page-level errors:

```text
Failure to load a page
    -> Error state inside the page

Failure to save a form
    -> Error near the form and optional notification

Unexpected application failure
    -> Global error handler

Authentication failure
    -> Authentication flow

Authorization failure
    -> Forbidden page or message
```

Do not handle every error only through snackbars.

A failed page load needs an error state with a retry action.

---

# 31. Notifications

Use a centralized notification service.

```typescript
@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(
      message,
      'Close',
      {
        duration: 4000
      }
    );
  }

  error(message: string): void {
    this.snackBar.open(
      message,
      'Close'
    );
  }
}
```

Use notifications for:

* Successful saves
* Successful deletion
* Background operation failure
* Non-blocking warnings

Do not use notifications as the only representation of:

* Form validation
* Page-loading errors
* Persistent security alerts
* Required user decisions

---

# 32. Tables and Pagination

Potentially large datasets must use backend pagination.

Examples:

* Devices
* DNS queries
* Connections
* Alerts
* Scan history

Request:

```typescript
export interface AlertQueryDto {
  page: number;
  pageSize: number;
  severity: AlertSeverity | null;
  status: AlertStatus | null;
  search: string | null;
  sortBy: AlertSortField;
  sortDirection: SortDirection;
}
```

Response:

```typescript
export interface PagedResultDto<T> {
  items: readonly T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

Rules:

* Sorting large datasets belongs to the backend.
* Filtering large datasets belongs to the backend.
* Pagination belongs to the backend.
* The frontend sends one query DTO.
* The backend returns one page DTO.
* Page and filter state should be reflected in the URL where useful.
* Search input should be debounced.
* A changed filter should normally reset the page to `1`.

---

# 33. Search and Filter Reactivity

Use RxJS for debounced user input.

Example:

```typescript
private readonly searchChanged =
  new Subject<string>();

readonly search = toSignal(
  this.searchChanged.pipe(
    map(value => value.trim()),
    debounceTime(300),
    distinctUntilChanged()
  ),
  {
    initialValue: ''
  }
);
```

Alternatively, convert a signal to an observable:

```typescript
readonly search = signal('');

readonly results = toSignal(
  toObservable(this.search).pipe(
    map(value => value.trim()),
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(search =>
      this.deviceApi.getDeviceList({
        ...this.query(),
        search
      })
    )
  ),
  {
    initialValue: null
  }
);
```

Use `switchMap` when a newer request should cancel the previous request.

---

# 34. Real-Time Updates

Real-time updates should use SignalR or another dedicated stream.

Real-time events must not cause uncontrolled full-page reloads.

Preferred flow:

```text
SignalR device-status event
    |
Facade
    |
Update affected device in signal state
    |
Computed state recalculates
    |
UI updates
```

Example:

```typescript
applyDeviceStatusUpdate(
  update: DeviceStatusChangedDto
): void {
  this._devices.update(devices =>
    devices.map(device =>
      device.id === update.deviceId
        ? {
            ...device,
            isOnline: update.isOnline,
            lastSeenUtc: update.lastSeenUtc
          }
        : device
    )
  );
}
```

The frontend must not establish separate real-time connections for every component.

Use one centralized real-time service and feature-level event handling.

---

# 35. Template Rules

Templates should remain declarative.

Use Angular control flow:

```html
@if (loading()) {
  <app-loading-state />
} @else if (error(); as error) {
  <app-error-state [error]="error" />
} @else if (device(); as device) {
  <app-device-summary [device]="device" />
}
```

Use:

```html
@for (device of devices(); track device.id) {
  <app-device-card [device]="device" />
}
```

Avoid:

* Calling expensive methods from templates
* Assigning values in templates
* Complex expressions
* Nested structural conditions that are difficult to read
* Repeating the same signal reads unnecessarily
* Business-rule calculations

Incorrect:

```html
@if (
  device().connections.filter(
    connection =>
      connection.isExternal &&
      connection.riskScore > 70
  ).length > 10
) {
  Suspicious
}
```

Correct:

```typescript
readonly hasCriticalActivity = computed(
  () => this.device()?.hasCriticalActivity ?? false
);
```

Ideally, the backend returns this classification directly.

---

# 36. Dependency Injection

Use the `inject()` function consistently in new code.

Example:

```typescript
export class DeviceListPageComponent {
  readonly facade = inject(DeviceListFacade);
  private readonly router = inject(Router);
}
```

Rules:

* Services should be provided at the narrowest reasonable scope.
* Global infrastructure uses `providedIn: 'root'`.
* Feature facades may be route- or page-scoped.
* Presentational components should rarely inject feature services.
* Do not use service locators.
* Do not store injectors globally.

---

# 37. Change Detection

All components must use:

```typescript
changeDetection: ChangeDetectionStrategy.OnPush
```

Signals naturally notify Angular when their consumed values change.

Rules:

* Do not manually call `detectChanges()` without a specific integration reason.
* Do not use `setTimeout()` to solve change-detection problems.
* Do not mutate arrays or objects in place.
* Use immutable updates.
* Ensure third-party imperative libraries are wrapped cleanly.

Incorrect:

```typescript
this.devices().push(newDevice);
```

Correct:

```typescript
this.devices.update(devices => [
  ...devices,
  newDevice
]);
```

---

# 38. Accessibility

Accessibility is mandatory.

Requirements:

* Use semantic HTML.
* Every form field has a visible label.
* Every icon-only button has an accessible label.
* Dialog focus must be managed.
* Keyboard navigation must work.
* Status must not be represented by color alone.
* Tables need meaningful headers.
* Dynamic loading and errors should be announced where necessary.
* Focus indicators must remain visible.
* Contrast must remain sufficient.
* Material components should not be modified in ways that break accessibility.

Example:

```html
<button
  mat-icon-button
  type="button"
  aria-label="Refresh device data"
  (click)="refreshRequested.emit()">
  <mat-icon>refresh</mat-icon>
</button>
```

---

# 39. Performance

The agent must consider:

* Lazy-loaded features
* One request per view
* Purpose-built backend DTOs
* Immutable state
* Stable tracking keys
* Backend pagination
* Avoiding unnecessary subscriptions
* Avoiding duplicate requests
* Avoiding oversized bundles
* Avoiding unnecessary Material imports
* Deferring heavy secondary content
* Virtual scrolling for very large local lists

Use `@defer` for non-critical expensive sections where appropriate.

Example:

```html
@defer (on viewport) {
  <app-device-traffic-chart
    [traffic]="traffic()" />
} @placeholder {
  <app-chart-placeholder />
}
```

Do not use deferred loading to hide poor backend endpoint design.

---

# 40. Subscription Management

Prefer:

* Signals
* `async` pipe
* `toSignal()`
* `takeUntilDestroyed()`
* Finite HTTP observables with `take(1)`

Example:

```typescript
private readonly destroyRef = inject(DestroyRef);

constructor() {
  this.realtimeService.deviceUpdates$
    .pipe(
      takeUntilDestroyed(this.destroyRef)
    )
    .subscribe(update => {
      this.applyDeviceUpdate(update);
    });
}
```

Do not keep manual arrays of subscriptions.

Incorrect:

```typescript
private subscriptions: Subscription[] = [];

ngOnDestroy(): void {
  this.subscriptions.forEach(
    subscription => subscription.unsubscribe()
  );
}
```

---

# 41. Naming Conventions

Use descriptive names.

Components:

```text
DeviceListPageComponent
DeviceTableComponent
DeviceStatusChipComponent
DeviceSummaryComponent
EditDeviceDialogComponent
```

Services:

```text
DeviceApi
DeviceFacade
DeviceStore
NotificationService
RealtimeService
```

DTOs:

```text
DeviceDto
DeviceListItemDto
DeviceDetailsPageDto
UpdateDeviceDto
DeviceQueryDto
```

Signals:

```text
devices
selectedDevice
loading
saving
error
query
filteredDevices
```

Outputs:

```text
deviceSelected
editRequested
deleteRequested
refreshRequested
pageChanged
filterChanged
```

Avoid vague names:

```text
data
item
model
result
manager
helper
common
utils
handleData
processData
doStuff
```

---

# 42. Testing

Every important feature must include tests.

## Component Tests

Test:

* Inputs
* Outputs
* Loading states
* Empty states
* Error states
* Conditional rendering
* User interactions
* Accessibility-critical behavior

## Facade Tests

Test:

* Initial state
* Successful loading
* Loading failure
* Saving state
* State update after mutation
* Duplicate request prevention
* Derived computed state

## API Client Tests

Test:

* HTTP method
* URL
* Request DTO
* Query parameters
* Response typing
* Error handling

## End-to-End Tests

Test critical flows:

```text
Open dashboard
Open device details
Search devices
Change trusted status
Resolve alert
Handle backend failure
Navigate using pagination
```

Do not test Angular Material internals.

Test the application contract and user-visible behavior.

---

# 43. New Page Checklist

Before implementing a page, the agent must determine:

## View Contract

* What data does the complete view require?
* Can the backend return it through one endpoint?
* Which response DTO represents the complete view?
* Which user actions can trigger writes?
* Which states must the page display?
* Does the URL need to preserve filters or pagination?

## Architecture

* Is this a new feature or part of an existing feature?
* Which page component owns the view?
* Which presentational components are required?
* Can existing shared components be reused?
* Is a feature facade required?
* Which state belongs in signals?
* Which asynchronous flow belongs in RxJS?

## Styling

* Which Material components are appropriate?
* Which existing layout pattern applies?
* Are existing design tokens sufficient?
* Does the page support desktop, tablet and mobile?
* Are all semantic statuses consistent?

## API

* Does opening the page trigger only one HTTP request?
* Is the API client typed?
* Is the response a proper view DTO?
* Is pagination handled by the backend?
* Are calculations handled by the backend?
* Can mutation responses update the UI without an extra GET?

---

# 44. New Component Checklist

Before creating a component:

* Does an equivalent shared component already exist?
* Is the component generic or feature-specific?
* Does it have one clear responsibility?
* Are its inputs typed and readonly?
* Do outputs describe user intentions?
* Does it avoid API access?
* Does it avoid backend business logic?
* Does it use Material components where possible?
* Does it use the centralized style guide?
* Does it use `OnPush`?
* Is it accessible?
* Is it responsive?
* Can it be tested independently?

---

# 45. Definition of Done

A frontend feature is complete only when:

* The initial view uses one HTTP request unless an exception is documented.
* The API response uses a purpose-built DTO.
* The frontend does not recreate backend business logic.
* Components do not call `HttpClient` directly.
* The API client is fully typed.
* No `any` types are introduced.
* The feature follows the defined folder structure.
* New components are standalone.
* Routes are lazy-loaded where appropriate.
* Page components remain thin.
* Presentational components use inputs and outputs.
* Signals are used for synchronous UI state.
* Computed state is not duplicated.
* RxJS is used for asynchronous stream composition.
* Effects are used only for genuine side effects.
* Duplicate requests are prevented.
* Loading, empty and error states are implemented.
* User actions cannot be submitted repeatedly while pending.
* Mutation responses are used without unnecessary reloads.
* Backend pagination is used for growing datasets.
* Reactive forms are typed.
* Material 3 components and design tokens are used.
* No arbitrary visual styles are introduced.
* The page is responsive.
* Accessibility requirements are met.
* Tests cover important behavior.

---

# 46. Forbidden Implementations

## Direct HTTP Calls in Components

```typescript
export class DevicePageComponent {
  private readonly http = inject(HttpClient);

  load(): void {
    this.http
      .get('/api/devices')
      .subscribe();
  }
}
```

## Multiple Calls to Construct One View

```typescript
forkJoin({
  device: this.deviceApi.getDevice(id),
  alerts: this.alertApi.getAlertsForDevice(id),
  dns: this.dnsApi.getQueriesForDevice(id),
  connections: this.connectionApi.getForDevice(id)
});
```

A dedicated backend view endpoint must be considered first.

## Backend Logic in the UI

```typescript
readonly severity = computed(() => {
  if (this.alert().score > 80) {
    return 'critical';
  }

  if (this.alert().score > 50) {
    return 'warning';
  }

  return 'low';
});
```

Severity must be supplied by the backend.

## Untyped API Data

```typescript
getDevices(): Observable<any> {
  return this.http.get<any>('/api/devices');
}
```

## Mutable Input Data

```typescript
this.device().displayName = newName;
```

## Imperative Derived State

```typescript
this.devices = response.items;
this.onlineDevices =
  response.items.filter(device => device.isOnline);
this.onlineCount = this.onlineDevices.length;
```

## Calling Methods From Templates

```html
@for (device of getFilteredDevices(); track device.id) {
}
```

## Generic Components With Domain Knowledge

```typescript
export class GenericTableComponent {
  private readonly alertApi = inject(AlertApi);
}
```

## Arbitrary Styles

```scss
.card {
  background: #192234;
  padding: 19px;
  border-radius: 11px;
}
```

## Repeated Material Overrides

```scss
::ng-deep .mat-mdc-button {
  // Feature-specific override
}
```

## Manual Subscription Collections

```typescript
private subscriptions: Subscription[] = [];
```

## Mutating Signal Collections

```typescript
this.devices().push(device);
```

## Effects for Derived State

```typescript
effect(() => {
  this.total.set(this.items().length);
});
```

---

# 47. Standard Feature Example

The device list page should follow this structure:

```text
DeviceListPageComponent
    |
DeviceListFacade
    |
DeviceApi
    |
GET /api/devices
    |
DeviceListPageDto
```

Response:

```typescript
export interface DeviceListPageDto {
  result: PagedResultDto<DeviceListItemDto>;
  summary: DeviceListSummaryDto;
  filterOptions: DeviceFilterOptionsDto;
}
```

Facade state:

```typescript
interface DeviceListState {
  data: DeviceListPageDto | null;
  loading: boolean;
  refreshing: boolean;
  error: ApiError | null;
}
```

Page component:

```typescript
@Component({
  selector: 'app-device-list-page',
  standalone: true,
  imports: [
    DeviceTableComponent,
    DeviceSummaryCardsComponent,
    DeviceFilterBarComponent,
    LoadingStateComponent,
    ErrorStateComponent,
    EmptyStateComponent
  ],
  providers: [DeviceListFacade],
  templateUrl: './device-list-page.component.html',
  styleUrl: './device-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeviceListPageComponent {
  readonly facade = inject(DeviceListFacade);

  onFilterChanged(query: DeviceQueryDto): void {
    this.facade.changeQuery(query);
  }

  onRefreshRequested(): void {
    this.facade.refresh();
  }
}
```

Template:

```html
<app-page-header
  title="Devices"
  description="Known and recently discovered network devices"
  (refreshRequested)="onRefreshRequested()" />

@if (facade.data(); as data) {
  <app-device-summary-cards
    [summary]="data.summary" />

  <app-device-filter-bar
    [options]="data.filterOptions"
    [query]="facade.query()"
    (queryChanged)="onFilterChanged($event)" />
}

@if (facade.loading()) {
  <app-loading-state />
} @else if (facade.error(); as error) {
  <app-error-state
    [error]="error"
    (retryRequested)="onRefreshRequested()" />
} @else if (facade.data(); as data) {
  @if (data.result.items.length === 0) {
    <app-empty-state
      title="No devices found"
      description="No devices match the current filters." />
  } @else {
    <app-device-table
      [devices]="data.result.items"
      (deviceSelected)="facade.openDevice($event)" />

    <app-pagination
      [page]="data.result.page"
      [pageSize]="data.result.pageSize"
      [totalCount]="data.result.totalCount"
      (pageChanged)="facade.changePage($event)" />
  }
}
```

One request returns everything required for the view.

The page coordinates the view.

The facade manages state and operations.

The API client handles HTTP communication.

The backend handles business rules and data aggregation.

Presentational components display typed data.

---

# 48. Instructions for Coding Agents

Whenever creating or modifying Angular code:

1. Inspect the existing feature structure before adding files.
2. Follow the established standalone architecture.
3. Use one HTTP request per initial view or explicit user action.
4. Request a backend view DTO instead of assembling screens through many calls.
5. Never place backend business logic in the frontend.
6. Never use `HttpClient` directly inside components.
7. Use typed DTOs for every request and response.
8. Never introduce `any`.
9. Place API communication in the feature API client.
10. Place feature coordination in a facade.
11. Use signals for synchronous state.
12. Use `computed()` for derived state.
13. Use RxJS for asynchronous stream composition.
14. Use `effect()` only for side effects.
15. Do not duplicate signal state.
16. Do not mutate arrays, objects or inputs.
17. Use standalone components.
18. Use `OnPush` change detection.
19. Lazy-load features.
20. Keep pages thin.
21. Keep presentational components independent from API services.
22. Reuse generic components before creating similar components.
23. Keep generic components free of domain logic.
24. Use Angular Material before implementing custom UI controls.
25. Follow the centralized Material 3 theme.
26. Use existing design tokens.
27. Implement loading, empty, error and success states.
28. Prevent duplicate clicks and duplicate requests.
29. Use mutation response DTOs instead of unnecessary reloads.
30. Use backend pagination for growing datasets.
31. Use typed reactive forms.
32. Preserve accessibility and responsive behavior.
33. Add or update relevant tests.
34. Do not introduce alternative state-management libraries without explicit approval.
35. Do not introduce NgRx, NGXS, Akita or another global store without a demonstrated need.
36. Do not introduce NgModules for new functionality.
37. Do not introduce custom styling patterns outside the shared style guide.
38. Do not bypass feature boundaries.
39. Do not perform calculations in templates.
40. Preserve the one-directional reactive data flow.

This architecture is the default and must be followed consistently for all new frontend functionality.
