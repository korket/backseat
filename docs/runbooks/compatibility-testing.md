# Compatibility testing runbook

Compatibility tests may interact with the real desktop and must be explicitly initiated by a human.

When using Cua Driver, also follow `docs/runbooks/cua-agent-safety.md`.

## Before testing

Record:

- backend permission mode / capability boundary;
- application name;
- executable path;
- process ID if relevant;
- window identifier if available;
- backend version;
- Windows version;
- whether recording is enabled.

Close or protect applications containing sensitive information when testing observation, input, or capture against the real desktop.

## Test matrix

For each target, test only the capabilities relevant to the experiment.

### Observation

- Can the target be captured while foregrounded?
- Can it be captured while unfocused?
- Can it be captured while covered?
- Can it be captured while minimized?

### Pointer actions

- Does a targeted click succeed?
- Does the physical cursor move?
- Does foreground focus change?
- What delivery route is reported?

### Keyboard actions

- Does typing succeed?
- Does the global keyboard state affect the user?
- Does foreground focus change?
- Are shortcuts handled differently from text?

### Application behavior

- Does the application continue rendering while unfocused?
- Does audio continue?
- Does it pause itself?
- Does it ignore background input?
- Does a child window or dialog change targeting behavior?

## Result template

Create a Markdown file under the appropriate `experiments/` directory and include:

```text
Application:
Version:
Engine/framework if known:
Backend:
Backend version:
PermissionMode:
CapabilityBoundary:
Target (exe/PID/window ID):
DeliveryRoute:

Observation:
Click:
Typing:
Foreground changed:
Physical cursor moved:
Minimized behavior:
Covered behavior:
Unexpected behavior:
Conclusion:
```

## Rule

A failed experiment is useful.

Do not "fix" an experiment by broadening permissions or enabling global input without documenting the change.
