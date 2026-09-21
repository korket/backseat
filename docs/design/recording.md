# Recording design

Status: Draft

## Goal

A Backseat run should be auditable after the session ends.

## Potential artifacts

```text
runs/<run-id>/
├── metadata.json
├── actions.jsonl
├── events.jsonl
├── recording.*
└── screenshots/
```

This layout is provisional.

## Principles

### Recording is separate from reasoning

The runtime records what happened. It does not need to understand why the agent chose an action.

### Structured logs first

Action and event logs are more useful for debugging than video alone.

### Avoid unbounded screenshot storage

Long-running computer-use sessions can generate large volumes of images.

Do not save every observation by default until retention and deduplication behavior has been designed.

### Backend recording is acceptable

Early milestones may delegate recording to an external backend.

Backseat should still own the association between recording artifacts and a run.

## Open questions

- Should video recording be mandatory or opt-in?
- Should action-linked screenshots be retained by default?
- What metadata is needed to synchronize actions with video?
- How should sensitive content be redacted or excluded?
