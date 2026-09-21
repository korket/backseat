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

- [ ] Add `SessionState`, `ActionRecord`, and `Session` to `Backseat.Core`.
- [ ] Enforce single-target authorization through backend discovery.
- [ ] Record observations and receipts in order with increasing sequence numbers.
- [ ] Implement lifetime cancellation and idempotent close.
- [ ] Cover lifecycle, authorization, logging, and cancellation with unit tests.

## Acceptance criteria

- [ ] Selecting a target that the backend does not report fails without changing state.
- [ ] A second target selection is rejected; early milestones keep one primary target.
- [ ] Observations and receipts are appended in order and never rewritten, including failed and non-background-safe results.
- [ ] Actions carry increasing sequence numbers starting at one.
- [ ] The session cancels its lifetime token on close and in-flight operations observe cancellation.
- [ ] Operations after close fail explicitly.
- [ ] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
