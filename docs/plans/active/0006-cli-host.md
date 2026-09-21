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

- [ ] Add `ObservationElement` and `ObservationElementFrame` and populate `Observation.Elements` from the driver payload.
- [ ] Add the `Backseat.Cli` project with `targets`, `observe`, and `act` verbs.
- [ ] Support `--runs <dir>` persistence and `--json` output.
- [ ] Cover element parsing and CLI argument/action parsing with unit tests.

## Acceptance criteria

- [ ] Observations carry structured elements with tokens, frames, actions, and depth.
- [ ] `backseat targets` lists discovered targets.
- [ ] `backseat observe --pid P [--window W]` prints the element list and tree summary, with optional `--screenshot PATH`.
- [ ] `backseat act` performs exactly one action and prints the receipt; failed receipts exit non-zero.
- [ ] `--runs DIR` writes a finalized run for the invocation.
- [ ] Usage errors exit with code 2 and a message.
- [ ] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
