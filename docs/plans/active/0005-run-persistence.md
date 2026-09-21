# Plan: Run persistence and recording

Status: Active

## Goal

Persist sessions as auditable runs (`runs/<run-id>/` with metadata, structured logs, and optional screenshots) and add the recording lifecycle so a session can delegate video recording to the backend and associate the artifact with the run.

## Non-goals

- No retention or deduplication policy for screenshots; they stay opt-in and unbounded retention is explicitly avoided.
- No replay tooling and no run browser UI.
- No cross-run indexing.
- No recording of the agent's reasoning; only runtime events.

## Current state

Contracts, the Cua CLI adapter, and the session lifecycle are merged (plans 0002-0004 completed). `docs/design/recording.md` sketches the artifact layout; the schema is intentionally unfrozen.

## Unknowns

- Whether JSONL event ordering is sufficient to synchronize actions with video without extra timestamps.
- Whether recording should survive session close or be torn down explicitly by policy.

## Tasks

- [ ] Add `IRunRecorder` and wire it into `Session` state, observation, action, and recording events.
- [ ] Add `RunWriter` producing `metadata.json`, `actions.jsonl`, `events.jsonl`, and opt-in `screenshots/`.
- [ ] Add `IRecordingBackend` and implement it in the Cua adapter (`start_recording` / `stop_recording`).
- [ ] Add `StartRecordingAsync` / `StopRecordingAsync` to `Session` with close-time teardown.
- [ ] Cover persistence, recording, and their failure paths with unit tests.

## Acceptance criteria

- [ ] A session with a `RunWriter` produces `metadata.json`, `actions.jsonl`, and `events.jsonl` under `runs/<run-id>/`.
- [ ] Observation screenshots are only written when explicitly enabled.
- [ ] Failed and non-background-safe receipts persist unchanged in `actions.jsonl`.
- [ ] Closing a session finalizes metadata with counts and final state.
- [ ] Recording delegates to the backend, records the artifact path, and refuses backends without recording support.
- [ ] Closing while recording attempts teardown and still closes.
- [ ] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
