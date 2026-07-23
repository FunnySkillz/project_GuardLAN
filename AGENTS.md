# GuardLAN Agent Instructions

This repository is intentionally split into two top-level workspaces:

- `GuardLAN.API` for the ASP.NET Core backend, worker, persistence, and backend architecture guidance.
- `GuardLAN.UI` for the Angular frontend and frontend architecture guidance.

When editing backend code, follow [GuardLAN.API/AGENTS.md](GuardLAN.API/AGENTS.md).

When editing Angular frontend code, follow [GuardLAN.UI/AGENTS.md](GuardLAN.UI/AGENTS.md).

## Feature Status Tracking

Before starting a major feature, review [docs/FEATURE_STATUS.md](docs/FEATURE_STATUS.md).

After implementing or materially changing a major feature, update its status, current state, implementation paths and next required change. The implementation in the repository is the source of truth. Do not update feature status based only on planned documentation.
