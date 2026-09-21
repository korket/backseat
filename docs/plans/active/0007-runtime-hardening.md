# Plan: Runtime hardening

Status: Active

## Goal

Harden observation and input delivery: retry degraded observations under a bounded settle policy, and enforce the foreground-input policy from `docs/decisions/0004-foreground-input-policy.md` at the session level so intrusive delivery can never pass silently.

## Non-goals

- No new backends.
- No change to the default background-first delivery contract.
- No automatic foreground escalation: the adapter only retries after the driver reports `background_unavailable` and only when explicitly enabled.

## Current state

The session, Cua adapter, run persistence, and CLI host are merged (plans 0002-0006 completed). The milestone smoke hit `ax_tree_empty` on a suspended app, and the adapter always sends `delivery_mode: background`.

## Unknowns

- Whether a short bounded retry is enough for suspended WinUI apps or whether a wake step is required.
- Whether the driver's `background_unavailable` payload shape is stable; detection must be conservative.

## Tasks

- [ ] Parse `degraded` and `degraded_reason` into observations.
- [ ] Add a bounded settle policy and apply it in `Session.ObserveAsync`.
- [ ] Add `DeliveryPolicy` with a session-level violation exception.
- [ ] Add opt-in foreground retry to the Cua adapter for `background_unavailable`.
- [ ] Cover settle and policy behavior with unit tests.

## Acceptance criteria

- [ ] A degraded observation is retried up to the settle limit and the final attempt is logged.
- [ ] A settled observation is returned without extra backend calls.
- [ ] A foreground receipt under the default policy is recorded and then raises a policy violation carrying the receipt.
- [ ] `AllowForeground` accepts a foreground receipt without complaint.
- [ ] The adapter retries once with foreground only when enabled and only on `background_unavailable`; the escalated receipt carries a warning.
- [ ] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
