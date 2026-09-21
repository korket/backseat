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

- [ ] Add the `Backseat.Backends.Cua` project and reference it from the solution.
- [ ] Add `ICuaCli` plus a process runner that passes tool arguments via stdin.
- [ ] Implement discovery, observation, and action execution with faithful receipt mapping.
- [ ] Cover the mapping with unit tests using a fake CLI.

## Acceptance criteria

- [ ] A background, confirmed result yields `ConfirmsBackgroundSafe`.
- [ ] An `unverifiable` result never claims background safety, even when delivery is background.
- [ ] A foreground result is recorded as foreground delivery and never claims background safety.
- [ ] Escalation hints are preserved as advisory `SuggestedEscalation`, not as success.
- [ ] Non-zero exits, refusals, and malformed output map to failed receipts with the original error text.
- [ ] `WaitAction` is handled locally without invoking the CLI.
- [ ] Discovery and observation parse the driver payloads and fail loudly on transport errors.
- [ ] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
