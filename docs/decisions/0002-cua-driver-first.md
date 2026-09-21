# ADR 0002: Evaluate Cua Driver before implementing native Windows automation

Status: Accepted

## Context

Backseat needs window targeting, observation, input delivery, and action evidence.

Existing open-source computer-use runtimes already implement parts of this problem. Reimplementing Windows input before compatibility testing would be premature.

## Decision

Use Cua Driver as the first backend evaluated by Backseat.

Keep it behind Backseat-owned interfaces.

Do not fork or modify Cua Driver during the initial compatibility milestone.

## Alternatives considered

- Implement Win32 input and capture immediately.
- Start with a full VM backend.
- Couple Backseat directly to Cua Driver APIs.

## Consequences

### Positive

- Faster evidence.
- Smaller first implementation.
- Existing backend behavior can be tested against real applications.

### Negative

- Early capability depends on an external project.
- Some applications may still require intrusive or isolated input strategies.
- Backseat must preserve backend diagnostics rather than hiding them.

## Revisit when

Cua Driver prevents a required Backseat capability or experiments show that a different backend is a better base.
