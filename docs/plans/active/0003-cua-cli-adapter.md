# Plan: Cua CLI adapter

Status: Active

## Goal

Implement `CuaCliBackend` behind `IComputerBackend`, driving the installed `cua-driver` CLI and mapping its JSON results into Backseat receipts without losing delivery, route, effect, or error evidence.

## Non-goals

- No MCP transport; this slice uses the CLI only.
- No session manager, run persistence, or recording orchestration.
- No automatic foreground escalation; the adapter only reports the driver's escalation hint, per `docs/decisions/0004-foreground-input-policy.md`.
- No retry policy and no target authorization logic.

## Current state

`Backseat.Core` contracts are merged (plan 0002 completed). Cua Driver 0.28.2 behavior is documented in `experiments/cua-driver/` and `docs/research/cua-driver.md`.

## Unknowns

- Whether the driver returns structured errors on stdout with a zero exit code for every failure class; the adapter must treat both exit codes and structured refusal objects as failures.
- How stable the receipt JSON field names are across driver versions.

## Tasks

- [x] Add the `Backseat.Backends.Cua` project and reference it from the solution.
- [x] Add `ICuaCli` plus a process runner that passes tool arguments via stdin.
- [x] Implement discovery, observation, and action execution with faithful receipt mapping.
- [x] Cover the mapping with unit tests using a fake CLI.

## Acceptance criteria

- [x] A background, confirmed result yields `ConfirmsBackgroundSafe`.
- [x] An `unverifiable` result never claims background safety, even when delivery is background.
- [x] A foreground result is recorded as foreground delivery and never claims background safety.
- [x] Escalation hints are preserved as advisory `SuggestedEscalation`, not as success.
- [x] Non-zero exits, refusals, and malformed output map to failed receipts with the original error text.
- [x] `WaitAction` is handled locally without invoking the CLI.
- [x] Discovery and observation parse the driver payloads and fail loudly on transport errors.
- [x] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 37 tests pass (22 core, 15 adapter).

## Discoveries

- Supervised read-only smoke against the live driver: discovery parsed 11-13 real windows, and observation decoded a 302 KB window PNG plus the UIA tree for a live window. No input actions were issued.
- Tool arguments go over stdin, which sidesteps the PowerShell positional-JSON quoting trap documented by the driver.
- The adapter always passes `delivery_mode: "background"` explicitly and never escalates; foreground remains a policy decision per ADR 0004.

## Decisions made during implementation

- Discovery and observation throw on transport failure; action execution returns failed receipts. Queries have no receipt channel, so silent empty results would hide a dead backend.
- `ScrollAction` carries an axis and signed ticks rather than free-form deltas, because the driver scrolls in named directions and tick counts.
