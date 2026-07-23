# GuardLAN MDAC Workspace

This folder is the starting point for the GuardLAN Mobile Device Activity Collector implementation.

The MDAC project is intentionally kept separate from the existing Angular UI and ASP.NET Core API so the mobile app can evolve independently while still sharing the same backend contract.

## Intended Scope

The initial implementation should focus on a small, consent-based MVP:

- device registration
- manual synchronization
- basic usage reporting
- simple backend ingestion
- privacy-friendly status and diagnostics

## Recommended Starting Approach

Start with a lightweight shared-app MVP before introducing native modules:

1. Create a React Native TypeScript app shell.
2. Add onboarding and device registration screens.
3. Add a manual sync flow that sends sample usage data to the GuardLAN API.
4. Add backend ingestion endpoints for registration and sync.
5. Only then add Android and iOS native collectors.

## Suggested Initial Structure

```text
GuardLAN.MDAC/
├── README.md
├── IMPLEMENTATION_PLAN.md
├── app/                  # React Native mobile app
├── backend/              # Optional local API helpers or contracts
└── docs/                 # MDAC-specific notes
```

## Next Step

See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for the recommended startup sequence.
