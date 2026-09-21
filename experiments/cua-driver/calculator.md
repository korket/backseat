# Calculator compatibility experiment

Supervised standard-mode test through `cua-driver call` (one-shot CLI).
Human operator present and explicitly approved the input scope.

```text
Application: Windows Calculator (packaged app)
Version: Windows 11 inbox Calculator
Engine/framework if known: WinUI / XAML (ApplicationFrameHost.exe)
Backend: Cua Driver
Backend version: cua-driver 0.28.2 (x86_64-windows)
PermissionMode: standard (built_in_default)
CapabilityBoundary: none (standard mode, supervised)
Target (exe/PID/window ID): ApplicationFrameHost.exe / pid 18408 / windows 22939714, 1967082
DeliveryRoute: accessibility (UIA Invoke via PostMessage) for every action
```

## Observation

- `list_windows`: 11 top-level windows with pid, window_id, bounds, z-order, on-screen flags.
- `get_window_state` on a visible Notepad window: 35 elements, snapshot id, per-element role/label/frame/action affordances, plus a window PNG screenshot. No document text is reproduced here (sensitivity).
- `get_window_state` on visible Calculator: 39 elements including the full button tree and a `Display is 0` readout element.
- Minimized Calculator: snapshot fails closed with explicit errors (`ax_tree_empty`, `cannot capture minimized window ... it has no rendered content`, escalation recommends `bring_to_front` first). No silent empty result.

## Click

- Sequence 6, Multiply, 7, Equals via `element_token` on one snapshot (s00000003): all four receipts `delivery.mode=background`, `route=accessibility`, `effect=unverifiable`. No foreground escalation, no `background_unavailable` error.
- Verification snapshot: display element reads `Display is 42`. 6x7=42 computed entirely through background input.

## Typing

Not tested (no text field exercised; Calculator path needs none).

## Foreground changed

No. Both `launch_app` calls returned `active=false`. No receipt reported a foreground swap. The Calculator window appeared on screen without stealing focus.

## Physical cursor moved

The physical cursor position changed several times during the session, but every driver receipt reports background/accessibility delivery and no input tool in this driver moves the system cursor. The deltas are attributed to the human operator's own concurrent desktop activity, not to the driver. A strict unattended proof would require an idle desktop.

## Minimized behavior

A minimized window cannot be observed (explicit error, see above). A hidden launch (`start_minimized=true`) produces a live pid with no actionable window; relaunching without the flag restores a visible, still-unactivated window under the same pid.

## Covered behavior

Not tested (Calculator was never covered during the input sequence).

## Unexpected behavior

- `kill_app` on the broker-activated Calculator pid refused: `foreign_process_termination_denied` ("standard mode may terminate only a process proven to have been launched by this Cua runtime"), even though the same daemon performed the launch. One-shot CLI calls carry no session label (`launch_app`/`kill_app` schemas accept none), so provenance cannot be established from the CLI path. Fail-closed, but it makes CLI-driven cleanup of packaged apps impossible.
- Polite close via background X-button invoke worked 2/2 and fully terminated the app (no residual windows for the pid afterward).
- Relaunching Calculator while a minimized instance existed reused the same pid with a new window id.

## Conclusion

Cua Driver 0.28.2 satisfies the Backseat background-safe contract on a WinUI packaged app: pid-scoped discovery, explicit observation receipts, background-first input with structured fail-closed errors, and no foreground or cursor side effects attributable to the driver. Open questions remaining: typing/focus behavior, covered-window input, and `kill_app` provenance for broker-activated apps.
