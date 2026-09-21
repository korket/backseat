# Session model

Status: Draft (the Created -> TargetSelected -> Active -> Closing -> Closed subset is implemented in `Backseat.Core.Session`; recording states remain unimplemented)

## Purpose

A Session is the main lifecycle boundary in Backseat.

It coordinates target authorization, backend resources, observations, actions, cancellation, recording, and run metadata.

## Proposed lifecycle

```text
Created
  |
  v
TargetSelected
  |
  v
Active <----> Recording
  |
  v
Closing
  |
  v
Closed
```

The exact state machine is not frozen.

## Ownership

A session should own:

- one backend session or connection;
- one authorized target set (target selection);
- observation history;
- action execution with sequence numbering;
- cancellation scope;
- optional recording lifecycle and recording state;
- run identity and run metadata.

A session should not own:

- AI model state;
- story or application-specific reasoning;
- provider credentials unrelated to the backend.

## Target changes

Early milestones should prefer one primary target per session.

Support for switching between arbitrary applications should only be introduced after explicit product requirements.

## Cancellation

Every backend operation that can block should be cancellable through the session lifetime token or equivalent mechanism.

Closing a session should attempt orderly cleanup even when the agent fails.
