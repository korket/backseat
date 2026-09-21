# Windows input experiments

Real-desktop tests must be explicitly initiated by a human and stay opt-in.

Target a specific process/window with explicit identifiers; record any fallback delivery route instead of silently accepting it.

Use only when existing backend behavior leaves an input-delivery question unanswered.

Every result must record:

- whether the physical cursor moved;
- whether foreground focus changed;
- whether the action actually reached the intended target;
- how success was verified.
