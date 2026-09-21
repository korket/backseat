# ADR 0001: Windows-first development

Status: Accepted

## Context

Backseat's first workload is local desktop application control, especially Windows visual novels and other Windows applications.

Trying to design cross-platform behavior before validating Windows input, capture, and isolation would add speculative abstraction.

## Decision

Backseat is Windows-first during early milestones.

Cross-platform design is allowed only where it naturally follows from an established boundary.

## Alternatives considered

- Cross-platform runtime from the first commit.
- Browser-only computer-use runtime.
- VM-only execution.

## Consequences

### Positive

- Faster compatibility experiments.
- Direct focus on the hardest current requirement.
- Less speculative abstraction.

### Negative

- Some early interfaces may need refinement if another platform is added.

## Revisit when

A supported non-Windows platform becomes an explicit product requirement.
