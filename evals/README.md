# Evals

Evals capture user-relevant Backseat behavior.

Unit tests answer whether code behaves correctly.

Evals answer whether Backseat can still accomplish a real computer-use scenario under stated constraints.

The eval framework is intentionally not implemented yet.

## Candidate scenarios

- observe a selected Notepad window;
- click a Calculator control without moving the user's cursor;
- type into an allowed target without foreground theft;
- advance a visual novel dialogue screen (compatibility target only, not core-runtime behavior);
- detect and report when a background-safe action is unavailable.

## Safety

Follow `docs/runbooks/cua-agent-safety.md` and `docs/runbooks/compatibility-testing.md`; success criteria must include cursor-moved, focus-changed, and delivery-route receipts.

## Rule

Do not invent an eval framework before at least one real scenario has stable semantics.
