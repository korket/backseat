# Design index

This directory contains deeper design documents.

Stable top-level boundaries belong in `ARCHITECTURE.md`.

## Documents

- `session-model.md` - session lifecycle and ownership.
- `computer-backends.md` - backend abstraction and capability model.
- `recording.md` - recording and run artifact direction.

## Design rule

Do not create a design document merely to predict future architecture.

A document should exist because:

- an implementation boundary is becoming stable;
- multiple components depend on the same contract;
- a platform experiment produced evidence that needs to guide implementation;
- a durable decision needs explanation.

Architectural decisions with meaningful alternatives should also receive an ADR in `docs/decisions/`.
