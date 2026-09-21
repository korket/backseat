# Plan: Action batches and screenshot retention

Status: Active

## Goal

Make agent sessions efficient and bounded: allow one `backseat_act` call to execute a small ordered batch of actions with per-action receipts, and bound run screenshot storage with retention plus duplicate suppression.

## Non-goals

- No parallel action execution; batches are strictly sequential.
- No branching or conditionals inside a batch; agents still observe and decide between calls.
- No video retention changes; recording artifacts stay owned by the backend.

## Current state

The MCP server, session, and run persistence are merged (plans 0007 and 0008 completed). `backseat_act` takes exactly one action, and `RunWriter` writes every enabled observation screenshot without a limit.

## Unknowns

- Whether agents benefit more from batching or from the observe-act-verify loop; batches must stay small so verification is not skipped.
- Whether duplicate suppression should compare against the previous screenshot only or a bounded window.

## Tasks

- [x] Accept an `actions` array in `backseat_act` with a hard batch cap and per-action receipts.
- [x] Report batch failures without losing the successful receipts.
- [x] Add screenshot retention (oldest-first deletion) and consecutive-duplicate suppression to `RunWriter`.
- [x] Cover batching and retention with unit tests.

## Acceptance criteria

- [x] A batch executes sequentially and returns one receipt per action, in order.
- [x] A failed receipt inside a batch marks the tool result as an error while still returning every receipt.
- [x] Batches larger than the cap are refused with an actionable message.
- [x] Retention keeps at most the configured number of observation screenshots, deleting the oldest.
- [x] Consecutive identical screenshots are not written twice.
- [x] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 122 tests pass (63 core, 26 adapter, 16 CLI, 17 MCP).

## Discoveries

- Batch cap is 50; each step is validated independently and a failed step still returns its receipt alongside the successful ones.
- Duplicate suppression compares against the previous screenshot hash only; a bounded comparison window remains an option if agents toggle between two screens.

## Decisions made during implementation

- Batches stay strictly sequential and receipt-per-step so the observe-act-verify loop remains possible; the agent still decides between calls.
- Retention defaults to 50 screenshots per run and is only active when screenshots are enabled.
