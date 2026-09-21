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

- [ ] Accept an `actions` array in `backseat_act` with a hard batch cap and per-action receipts.
- [ ] Report batch failures without losing the successful receipts.
- [ ] Add screenshot retention (oldest-first deletion) and consecutive-duplicate suppression to `RunWriter`.
- [ ] Cover batching and retention with unit tests.

## Acceptance criteria

- [ ] A batch executes sequentially and returns one receipt per action, in order.
- [ ] A failed receipt inside a batch marks the tool result as an error while still returning every receipt.
- [ ] Batches larger than the cap are refused with an actionable message.
- [ ] Retention keeps at most the configured number of observation screenshots, deleting the oldest.
- [ ] Consecutive identical screenshots are not written twice.
- [ ] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
