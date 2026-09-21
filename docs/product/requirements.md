# Product requirements

These requirements describe desired behavior. They do not imply that every requirement is implemented in the first milestone.

## Core requirements

### R1: Target selection

Backseat must be able to identify and target a specific desktop application and, where possible, a specific window.

### R2: Observation

Backseat must provide an agent with observations of the selected target.

At minimum, the target backend should eventually support screenshots.

Semantic accessibility information is optional and backend-dependent.

### R3: Action execution

Backseat must support a small set of computer actions:

- click;
- type text;
- press key;
- scroll;
- wait.

Additional actions may be added from demonstrated need.

### R4: Background-safe behavior

When a backend can operate a target without moving the human's physical cursor or stealing foreground focus, Backseat should preserve that behavior.

### R5: No silent unsafe fallback

Backseat must not silently replace a background-targeted action with global system input.

### R6: Action receipts

Each meaningful action must return enough information to determine what happened.

Receipts should preserve backend evidence such as:

- success or failure;
- action route;
- foreground change;
- cursor movement;
- warnings;
- error details.

### R7: Cancellation

Long-running sessions and backend calls must be cancellable.

### R8: Recording

Backseat should support recording a session and associating the recording with the run.

The first implementation may delegate recording to an external backend.

### R9: Run history

Backseat should be able to persist a structured record of a run, including actions and notable events.

### R10: Agent independence

The core runtime must not require a specific AI model provider.

### R11: Backend independence

Application logic must not depend directly on a specific computer-use backend.

### R12: Opt-in intrusive testing

Tests that can interact with the real desktop must be explicit and opt-in.

## First milestone acceptance criteria

The compatibility milestone is successful when we can demonstrate, against at least one real Windows application:

1. exact target discovery;
2. one observation;
3. one background-targeted action;
4. a trustworthy action receipt;
5. no unreported cursor movement or focus theft;
6. clean session shutdown.

A visual novel should then be used as the first application compatibility experiment.
