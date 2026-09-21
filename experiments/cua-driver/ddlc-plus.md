# Doki Doki Literature Club Plus compatibility experiment

Supervised standard-mode test through `cua-driver call` (one-shot CLI).
The human operator approved the launch, observation, background input attempt, and recording smoke test.

```text
Application: Doki Doki Literature Club Plus (Steam install)
Version: current
Engine/framework if known: Unity (custom-rendered; no UIA content tree)
Backend: Cua Driver
Backend version: cua-driver 0.28.2 (x86_64-windows)
PermissionMode: standard (built_in_default)
CapabilityBoundary: none (standard mode, supervised)
Target (exe/PID/window ID): Doki Doki Literature Club Plus.exe / pid 2288 / window 6294256
DeliveryRoute: synthetic_events for click/press_key; accessibility for frame-button invoke
```

## Observation

- `get_window_state`: only the native frame is exposed (TitleBar, System, Minimize, Close). The game content has no accessibility elements, as expected for Unity.
- Screenshots returned live frames: the login screen first, then the main menu with rendered content; the in-game clock advanced across successive captures (7:04 PM -> 7:09 PM).
- The window sat beneath four opaque full-screen windows in z-order (z=6 vs z=8..11) during capture, and frames still rendered. Strong evidence for capture-while-covered on a DirectX surface. Caveat: virtual-desktop membership was not verified, so treat as strong, not conclusive.
- Minimized: not re-tested here (see `calculator.md`; expect the same fail-closed behavior).

## Click

- Pixel clicks with default background mode: receipt `delivery.mode=background`, `route=synthetic_events`, `effect=unverifiable`.
- Targeted sidebar entries (Files, Settings) and mid-canvas points produced zero visual change across captures. Input did not reach the renderer.
- The driver did NOT return `background_unavailable`; it returned plausible `unverifiable` receipts. Effect confirmation is therefore mandatory and cannot be inferred from the receipt.
- Not counted as evidence: one click near the login label coincided with a screen transition, but the operator's own concurrent input cannot be excluded.

## Typing

Not tested (no text field reached).

## Special keys

- `press_key` Escape: `route=synthetic_events`, `effect=unverifiable`, `escalation={reason=delivery_failed, target=foreground}`. No visual change.
- Note the contrast with Notepad, where the same `delivery_failed` hint fired even though the key did land. The hint is advisory and unverifiable.

## Foreground changed

No. No receipt reported a foreground swap. Foreground escalation was NOT exercised (would steal focus; requires explicit human approval).

## Physical cursor moved

No driver-attributable movement. All receipts report background/synthetic delivery; cursor deltas tracked the operator's own activity.

## Recording smoke test

- `start_recording` with `record_video=true` and an absolute temp `output_dir`. ffmpeg 8.0.1 present on PATH.
- Produced a valid `recording.mp4`: H.264, 1920x1080, 30 fps. `session.json` reports finalized video and a cursor sample count; `cursor.jsonl` present.
- Video captures the main display (full desktop), not just the target window.
- CRITICAL: recording is owned by the transport process. With one-shot CLI calls the recording was finalized the moment `start_recording` returned: video duration ~1.6 s, `enabled=false` immediately afterward, and no `turn-00001/` folders were produced for subsequent clicks. Trajectory recording requires a persistent client (MCP session) held open for the whole run.

## Unexpected behavior

- Background input silently dropped with a plausible receipt (no `background_unavailable`).
- `kill_app` refused (`foreign_process_termination_denied`) for a process launched via `launch_app` from a different CLI invocation - same provenance gap as Calculator.
- Frame Close-button invoke (`route=accessibility`) did not terminate the game, unlike Calculator/Notepad. System-menu expand also did not surface a native menu through background delivery.
- As a result the game could not be closed through any driver path; cleanup requires the human operator.
- No bypass was attempted: the driver's refusal was not worked around with OS-level termination.

## Conclusion

Observation of a custom-rendered Unity visual novel works while the window is unfocused and stacked beneath other windows. Background input does not: this engine drops synthetic PostMessage events, and the driver reports `unverifiable` rather than a structured failure. Playable unattended background operation of this VN is NOT demonstrated. The remaining route is foreground escalation (focus steal), which conflicts with Backseat's "alongside the user" goal and needs an explicit policy decision. Recording infrastructure works, but trajectory recording requires a persistent MCP session; the CLI path cannot record a session.
