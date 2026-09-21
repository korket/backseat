# Plan: CLI host and structured elements

Status: Active

## Goal

Ship the first real entry point, `backseat`, covering target discovery, observation, and single-action execution with optional run persistence. Expose the driver's structured element list (role, label, token, frame) on observations so the CLI and future agents can address elements without re-snapshotting through raw tools.

## Non-goals

- No script language, no agent loop, and no MCP/HTTP surface.
- No interactive TUI.
- No multi-action batching; one invocation performs one action.
- No foreground escalation; the CLI uses background delivery only, per `docs/decisions/0004-foreground-input-policy.md`.

## Current state

Contracts, the Cua adapter, the session lifecycle, and run persistence are merged (plans 0002-0005 completed). Observations expose the raw tree and screenshot but not element tokens; the milestone smoke had to fetch tokens through the runner.

## Unknowns

- Whether agents need the full element list or a filtered view; the raw list is the starting point.
- Which CLI ergonomics hold up once an agent, not a human, drives the host.

## Tasks

- [x] Add `ObservationElement` and `ObservationElementFrame` and populate `Observation.Elements` from the driver payload.
- [x] Add the `Backseat.Cli` project with `targets`, `observe`, and `act` verbs.
- [x] Support `--runs <dir>` persistence and `--json` output.
- [x] Cover element parsing and CLI argument/action parsing with unit tests.

## Acceptance criteria

- [x] Observations carry structured elements with tokens, frames, actions, and depth.
- [x] `backseat targets` lists discovered targets.
- [x] `backseat observe --pid P [--window W]` prints the element list and tree summary, with optional `--screenshot PATH`.
- [x] `backseat act` performs exactly one action and prints the receipt; failed receipts exit non-zero.
- [x] `--runs DIR` writes a finalized run for the invocation.
- [x] Usage errors exit with code 2 and a message.
- [x] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 87 tests pass (50 core, 21 adapter, 16 CLI).

## Discoveries

- Live smoke: `targets` listed 11 windows, `observe --elements` printed tokens and frames for a live window, and `act --wait 100 --runs runs` produced a finalized run (`metadata.json` with counts, `actions.jsonl` with the receipt).
- Usage errors exit 2; runtime failures exit 1; failed receipts exit 1 with the receipt printed.

## Decisions made during implementation

- The CLI resolves a pid-only target to the first discovered window so callers do not have to copy window ids for simple cases.
- One invocation performs one action; batching and scripting are deliberately deferred to the future agent surface.
