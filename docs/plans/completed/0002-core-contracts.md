# Plan: Core backend contracts

Status: Active

## Goal

Add the first production Backseat code: backend-agnostic contracts for targets, observations, actions, receipts, and capabilities, plus unit tests. These contracts sit between the session model and any computer backend so a Cua adapter can be added later without leaking backend types.

## Non-goals

- No Cua adapter implementation.
- No session manager, recording, or run persistence.
- No CLI or GUI.
- No foreground-escalation enforcement code; that follows `docs/decisions/0004-foreground-input-policy.md`.
- No Win32 or input implementation of any kind.

## Current state

The repository is documentation-only. Compatibility evidence lives in `experiments/cua-driver/`; the receipt semantics (delivery mode, route, effect, unknown-versus-false) come from that evidence.

## Unknowns

- Whether the receipt shape survives contact with the Cua adapter; expect refinement rather than redesign.
- Which capability flags are needed once a second backend exists.

## Tasks

- [x] Add the solution and the `Backseat.Core` project.
- [x] Define `TargetDescriptor`, `Observation`, `ComputerAction` variants, `ActionReceipt`, and `BackendCapabilities`.
- [x] Define `IComputerBackend` with discovery, observation, and execution.
- [x] Add unit tests for the receipt and target invariants.

## Acceptance criteria

- [x] Contracts compile under .NET 10 with nullable reference types enabled.
- [x] Receipts distinguish "unknown" from "false" for foreground and cursor evidence.
- [x] No receipt can claim background-safe delivery without confirmed background delivery.
- [x] Target identity requires an explicit process id.
- [x] Click actions require exactly one address form (coordinates or element handle).
- [x] The backend interface is implementable without referencing backend-specific types.
- [x] `dotnet test` passes and `pwsh ./scripts/verify.ps1` runs the solution pipeline.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 20 tests pass.

## Discoveries

- `dotnet new xunit` pinned package versions that this machine's feeds do not carry; the test project now pins `Microsoft.NET.Test.Sdk` 18.0.1 and `xunit.runner.visualstudio` 3.1.5, and drops `coverlet.collector`.
- Click addressing uses overloaded constructors rather than init-property validation, because record init accessors cannot enforce exactly-one-of at construction time.

## Decisions made during implementation

- `ActionReceipt.SuggestedEscalation` is advisory only and never implies success, matching the observed `escalation` hint behavior.
