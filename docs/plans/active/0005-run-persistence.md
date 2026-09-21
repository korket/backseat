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

- [x] Add `IRunRecorder` and wire it into `Session` state, observation, action, and recording events.
- [x] Add `RunWriter` producing `metadata.json`, `actions.jsonl`, `events.jsonl`, and opt-in `screenshots/`.
- [x] Add `IRecordingBackend` and implement it in the Cua adapter (`start_recording` / `stop_recording`).
- [x] Add `StartRecordingAsync` / `StopRecordingAsync` to `Session` with close-time teardown.
- [x] Cover persistence, recording, and their failure paths with unit tests.

## Acceptance criteria

- [x] A session with a `RunWriter` produces `metadata.json`, `actions.jsonl`, and `events.jsonl` under `runs/<run-id>/`.
- [x] Observation screenshots are only written when explicitly enabled.
- [x] Failed and non-background-safe receipts persist unchanged in `actions.jsonl`.
- [x] Closing a session finalizes metadata with counts and final state.
- [x] Recording delegates to the backend, records the artifact path, and refuses backends without recording support.
- [x] Closing while recording attempts teardown and still closes.
- [x] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 69 tests pass (50 core, 19 adapter).

## Discoveries

- Windows share modes: a JSONL file open for append cannot be read back with `File.ReadAllLines` unless both sides use `FileShare.ReadWrite`. The writer and the test readers now do, which also models live tailing.
- `Session` accepts an optional id so the run directory and the session identity can be the same value.
- Live recording through the session was not re-run against the driver because it captures the whole desktop; the raw CLI recording path was verified earlier in `experiments/cua-driver/ddlc-plus.md`.

## Decisions made during implementation

- Run metadata is rewritten on close with counts, final state, and the recording artifact path, while JSONL logs stay append-only.
- Recorder failures propagate: losing an audit trail silently is worse than failing the call. In-memory logs are appended before the recorder runs, so a failed write never erases the record.
- Recording is modeled as a flag within `Active` rather than a separate state; the design doc leaves the exact machine unfrozen.
