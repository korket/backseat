# Cua Driver research

Status: To verify locally

## Why it matters

Cua Driver is the first external backend Backseat plans to evaluate.

## Questions to answer

- What stable interfaces are available for target discovery?
- What observation data is available?
- Which input actions can be background-safe on Windows?
- What evidence is returned for cursor movement or foreground changes?
- What recording support exists?
- How are game-like or custom-rendered windows handled?
- Which features are available through MCP versus direct SDK/runtime APIs?
- What failure modes matter for long-running sessions?

## Local findings

Record only behavior verified from current documentation, source, or experiments.

### Environment

- Version: cua-driver 0.28.2 (x86_64-windows)
- Installation method: `irm https://cua.ai/driver/install.ps1 | iex`
- Windows version: Windows 11 Pro 10.0.26200 64-bit
- Permission mode: standard (supervised test; bounded manifest not yet configured)
- Capability manifest (if bounded): n/a

### Target discovery

Verified: `list_windows` returns pid, window_id, bounds, z-order, and on-screen flags; `list_apps` returns running/installed catalog plus live processes.

### Observation

Verified on Notepad and Calculator: `get_window_state` returns a structured element tree (role/label/frame/affordances), snapshot id, and window PNG. Minimized windows fail closed with explicit errors (no silent empty results).

### Click behavior

Verified on Calculator 6x7=42: all clicks `delivery.mode=background` over the accessibility route; no foreground escalation. Result confirmed via re-snapshot (`Display is 42`).

On a custom-rendered Unity VN (DDLC Plus) background pixel clicks reported `unverifiable` and had no effect; the driver did not return `background_unavailable`. Background input is not reliable for custom-rendered targets and must be confirmed by re-observation.

### Keyboard behavior

Verified on Notepad: `type_text` via UIA ValuePattern returned `confirmed` with read-back evidence; re-snapshot matched. `press_key` Return landed but its receipt said `unverifiable` with a foreground-escalation hint (false negative on a deferred XAML provider). Lesson: re-snapshot to verify; never auto-escalate on `unverifiable`.

### Recording

Verified: `start_recording` with `record_video=true` produces a valid H.264 1920x1080 30fps full-display mp4 via ffmpeg, plus `session.json` and `cursor.jsonl`.

Limitation: recording is owned by the transport process. One-shot CLI calls finalize the recording as soon as the `start_recording` command exits and produce no per-turn trajectory folders. Trajectory recording requires a persistent MCP client session.

### Known limitations

- `kill_app` refused a broker-activated packaged-app pid from one-shot CLI calls (`foreign_process_termination_denied`); polite X-button close worked instead.
- Same provenance gap for a Steam game launched via `launch_app` from a different CLI invocation; no driver path could close it.
- Click receipts report `effect=unverifiable`; success must be confirmed by re-snapshot.
- On custom-rendered targets (Unity), background synthetic input can be silently dropped while receipts still report `unverifiable` (not `background_unavailable`). Re-observation is the only reliable signal.
- Recording video captures the whole display, not just the target window.
- Cursor-movement attribution needs an idle desktop for a strict proof.

## Implications for Backseat

Evidence so far supports the Backseat background-safe contract (explicit targets, intent vs receipt, no silent fallback). Full detail lives in `experiments/cua-driver/calculator.md`.
