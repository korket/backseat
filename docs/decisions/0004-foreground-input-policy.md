# ADR 0004: Foreground escalation requires explicit policy, never automatic fallback

Status: Proposed

## Context

Compatibility evidence from `experiments/cua-driver/` shows two classes of target:

- UIA-rich applications (Calculator, Notepad) support background-safe input: clicks come back as `background` / `accessibility` with no foreground swap, and typing returns a `confirmed` value read-back.
- Custom-rendered applications (DDLC Plus, Unity) expose no accessibility content. Background input is silently dropped while receipts still report `unverifiable`, and the driver does not return a structured `background_unavailable`. A single approved foreground escalation on the same target succeeded, proving the coordinates were correct and the background path was the failure.

`ARCHITECTURE.md` already forbids silently converting a background-targeted action into global user input, and allows either rejection or an explicit receipt. What remains undecided is whether Backseat should ever permit foreground-intrusive input at all, and under what conditions.

## Decision

Proposed:

1. Backseat never escalates from background to foreground delivery automatically. A backend failure never grants permission to take the user's focus or cursor.
2. Foreground-intrusive delivery is available only under an explicit session policy chosen before the session starts, and only for attended or supervised sessions.
3. Every intrusive action produces an ActionReceipt that names the delivery mode, whether foreground changed, and whether the physical cursor moved.
4. Background-safe claims are made only from backend evidence; `unverifiable` effects are resolved by re-observation and never reported as success.
5. The default policy rejects intrusive delivery and reports the limitation to the agent.

## Alternatives considered

- Always reject foreground input. Simplest and safest, but makes any target that drops background input permanently unplayable, including the visual-novel workload.
- Automatic escalation when background delivery fails. Rejected: it violates the safety invariant in `ARCHITECTURE.md` and silently consumes the user's focus.
- Delegate the decision entirely to the backend's permission mode. Rejected: the Cua safety runbook already states that a permissive backend is not permission for the agent to roam the desktop.
- Require an isolated-session or VM backend for intrusive targets. Deferred: much larger implementation; only justified if the policy cannot deliver the workload.

## Consequences

### Positive

- The safety invariant stays intact and audit-able.
- Visual-novel workloads remain reachable in attended sessions without weakening the unattended default.
- Receipts remain the single source of truth for background-safety claims.

### Negative

- Unattended visual-novel play is not supported by this policy.
- A per-session intrusive policy is a new product surface that must be designed, recorded, and tested.
- Verification-by-observation costs extra snapshot round-trips on targets with `unverifiable` receipts.

## Revisit when

- An isolated-session or VM-backed approach becomes viable.
- A backend delivers reliable background input for custom-rendered targets.
- Product requirements demand unattended operation on targets that require intrusive input.
