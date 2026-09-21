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

### Keyboard behavior

TBD.

### Recording

TBD.

### Known limitations

- `kill_app` refused a broker-activated packaged-app pid from one-shot CLI calls (`foreign_process_termination_denied`); polite X-button close worked instead.
- Click receipts report `effect=unverifiable`; success must be confirmed by re-snapshot.
- Cursor-movement attribution needs an idle desktop for a strict proof.

## Implications for Backseat

Evidence so far supports the Backseat background-safe contract (explicit targets, intent vs receipt, no silent fallback). Full detail lives in `experiments/cua-driver/calculator.md`.
