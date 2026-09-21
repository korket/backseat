# Plan: Session lifecycle

Status: Active

## Goal

Implement the `Session` runtime object: one authorized target per session, action sequencing, observation and receipt logging, and cancellation with orderly close, all behind the `IComputerBackend` contract.

## Non-goals

- No recording lifecycle yet; the design keeps `Active <-> Recording` for a later slice.
- No multi-target switching, no target policy language, and no foreground-escalation policy enforcement beyond what receipts already carry.
- No run persistence to disk; logs are in-memory for now.
- No CLI or agent integration.

## Current state

Contracts and the Cua CLI adapter are merged (plans 0002 and 0003 completed). The session model draft is `docs/design/session-model.md`.

## Unknowns

- Whether a separate explicit activation step is needed, or first use should activate.
- Whether backend disposal belongs on the session or the caller once a second backend exists.

## Tasks

- [x] Add `SessionState`, `ActionRecord`, and `Session` to `Backseat.Core`.
- [x] Enforce single-target authorization through backend discovery.
- [x] Record observations and receipts in order with increasing sequence numbers.
- [x] Implement lifetime cancellation and idempotent close.
- [x] Cover lifecycle, authorization, logging, and cancellation with unit tests.

## Acceptance criteria

- [x] Selecting a target that the backend does not report fails without changing state.
- [x] A second target selection is rejected; early milestones keep one primary target.
- [x] Observations and receipts are appended in order and never rewritten, including failed and non-background-safe results.
- [x] Actions carry increasing sequence numbers starting at one.
- [x] The session cancels its lifetime token on close and in-flight operations observe cancellation.
- [x] Operations after close fail explicitly.
- [x] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 51 tests pass (36 core, 15 adapter).

## Discoveries

- Supervised live smoke (read-only plus one local wait): session discovered 12 windows, selected a live window through backend authorization, observed a 620 KB screenshot, executed a local wait receipt (`Confirmed`, route `local`, `ConfirmsBackgroundSafe=False`), and closed cleanly with `CancellationToken` cancelled and both logs populated.
- Backend disposal uses an optional `IAsyncDisposable` check so the contract stays minimal until a second backend needs an explicit close path.
- The first observation or action activates the session; no separate activation call is needed for the current lifecycle.
- `CancellationTokenSource.Dispose` in `DisposeAsync` means callers should read `CancellationToken` before disposal; `CloseAsync` alone leaves the token readable.

## Decisions made during implementation

- Target authorization is discovery-based: the session only accepts a target the backend itself reports, and stores the backend's descriptor as authoritative.
- Cancelled in-flight actions are not recorded; only receipts that came back are logged.
