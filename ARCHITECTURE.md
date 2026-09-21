# Backseat architecture

This document describes stable system boundaries. Detailed implementation belongs in `docs/design/`.

## System model

```text
                   AI Agent
                      |
                Agent Adapter
                      |
                Backseat API
                      |
                Session Manager
                      |
       +--------------+--------------+
       |              |              |
   Observation      Actions       Recording
       |              |              |
       +--------------+--------------+
                      |
              Computer Backend
                      |
          +-----------+-----------+
          |                       |
     Existing backend        Future native /
      such as Cua             isolated backend
```

## Primary boundary

Backseat does not own model reasoning.

The runtime receives requested computer actions and returns observations and action receipts. An agent may run in-process, out-of-process, through MCP, through an HTTP API, or through another adapter without changing the core computer backend contract.

## Core concepts

### Session

A long-lived execution context that owns:

- target selection;
- backend connection;
- observation history;
- action execution;
- cancellation;
- recording state;
- run metadata.

### Target

The exact application surface the agent is allowed to control.

A target may include:

- process identity;
- window identity;
- executable metadata;
- backend-specific identifiers.

Target identity must be explicit. Human-readable window titles alone are not sufficient when stronger identifiers are available.

### Observation

A timestamped snapshot of what the agent can perceive.

Possible fields include:

- screenshot;
- target metadata;
- accessibility tree or semantic elements;
- backend state;
- recording state.

Backends may support different observation capabilities.

### Action

A requested operation, such as:

- click;
- type text;
- press key;
- scroll;
- wait;
- focus target;
- start or stop recording.

Actions express intent. They do not imply that a requested delivery method succeeded.

### ActionReceipt

The authoritative result of an action.

A receipt should preserve, when available:

- success or failure;
- delivery route;
- whether foreground changed;
- whether the physical cursor moved;
- target identity;
- backend warnings;
- timing;
- error details.

Backseat must not erase backend evidence that an operation was unsafe, degraded, or unsupported.

### Run

A persisted record of a session.

A future run layout may include:

```text
runs/<run-id>/
├── metadata.json
├── actions.jsonl
├── events.jsonl
├── recording.*
└── screenshots/
```

The exact schema is intentionally not frozen yet.

## Backend boundary

Backseat should be able to support multiple computer backends.

The first implementation may use an existing backend, but application code must depend on Backseat abstractions rather than backend-specific types.

Potential backends:

- Cua Driver;
- another external computer-use runtime;
- a native Windows implementation;
- an isolated-session implementation;
- a VM-backed implementation.

## Safety invariant

No backend may silently convert a background-targeted action into global user input.

If a backend requires foreground activation, physical cursor movement, global keyboard injection, or another Intrusive input fallback (see `docs/product/terminology.md`), Backseat must either:

1. reject the action under the current policy; or
2. return an explicit receipt showing what happened.

## Current exclusions

The core architecture does not currently include:

- model training;
- autonomous route planning;
- visual novel parsing;
- OCR implementation;
- hypervisor management;
- multi-agent orchestration;
- cloud execution.
