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

- [x] Parse `degraded` and `degraded_reason` into observations.
- [x] Add a bounded settle policy and apply it in `Session.ObserveAsync`.
- [x] Add `DeliveryPolicy` with a session-level violation exception.
- [x] Add opt-in foreground retry to the Cua adapter for `background_unavailable`.
- [x] Cover settle and policy behavior with unit tests.

## Acceptance criteria

- [x] A degraded observation is retried up to the settle limit and the final attempt is logged.
- [x] A settled observation is returned without extra backend calls.
- [x] A foreground receipt under the default policy is recorded and then raises a policy violation carrying the receipt.
- [x] `AllowForeground` accepts a foreground receipt without complaint.
- [x] The adapter retries once with foreground only when enabled and only on `background_unavailable`; the escalated receipt carries a warning.
- [x] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 115 tests pass (60 core, 26 adapter, 16 CLI, 13 MCP).

## Discoveries

- The driver reports `degraded: true` with `degraded_reason: ax_tree_empty` on suspended packaged apps, which is the signal the settle policy keys on.
- `background_unavailable` arrives as an error string; detection is substring-based and deliberately conservative.

## Decisions made during implementation

- Settle retries apply in the session, not the adapter, so any backend can opt in by setting `IsDegraded`.
- A policy violation is loud but non-destructive: the receipt is recorded first, then raised inside the exception so the caller cannot lose it.
