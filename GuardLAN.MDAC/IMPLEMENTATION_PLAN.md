# MDAC Implementation Plan

This plan is meant to start the MDAC work in a practical and low-risk way.

## Phase 1 — Establish the workspace

Create the initial MDAC workspace structure:

- [GuardLAN.MDAC/README.md](README.md)
- [GuardLAN.MDAC/IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)
- a React Native app folder
- an MDAC-specific docs folder

## Phase 2 — Build the shared mobile shell

Create a minimal React Native + TypeScript app with:

- onboarding screen
- device registration form
- server URL configuration
- manual sync button
- basic status screen

The first goal is not full platform integration. The first goal is a working app shell that can talk to the GuardLAN API.

## Phase 3 — Define the first API contract

Add the first backend-facing contract for MDAC:

- register device
- submit usage snapshot
- report sync status

Keep the payloads small and explicit. Avoid trying to model every future capability in the first iteration.

## Phase 4 — Add backend ingestion support

In the GuardLAN API, add a small MDAC ingestion area that can:

- receive device registrations
- store basic usage snapshots
- relate the agent to a known device or device identity
- return a simple success response

## Phase 5 — Introduce native collectors

Once the shared app and backend contract are working, add platform-specific collectors:

- Android: usage statistics and background sync hooks
- iOS: screen-time and usage access hooks where permitted

This step should only begin once the app shell and API contract are stable.

## Recommended First Deliverable

The best first milestone is:

- an app that can register a device
- a backend endpoint that accepts the registration
- a manual sync action that sends a simple usage event

This gives the project a clear foundation without overcommitting to native complexity too early.

## Suggested Repository Integration

Keep MDAC as a sibling workspace under the repository root:

```text
GuardLAN.MDAC/
```

It should remain independent from the existing Angular UI and API directories, but it should reuse the GuardLAN backend contract and share the same overall product goals.
