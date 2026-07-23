# GuardLAN Web Agent Instructions

When creating or modifying Angular frontend functionality in this folder, follow [docs/FRONTEND_ARCHITECTURE.md](docs/FRONTEND_ARCHITECTURE.md).

Key defaults:

- Use standalone Angular components with `OnPush` change detection.
- Keep page components thin and coordinate feature behavior through facades.
- Put typed API access in feature API clients; components must not inject `HttpClient` directly.
- Use signals for synchronous UI state and RxJS for asynchronous stream composition.
- Prefer one backend request per initial view or explicit user action.
- Use typed DTOs, avoid `any`, preserve accessibility, and keep growing datasets backend-paginated.
- Do not introduce NgModules, global state libraries, or alternate frontend architecture patterns without explicit approval.
