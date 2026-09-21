# Plan: Cua Driver compatibility spike

Status: Active

## Goal

Determine whether an existing Cua Driver installation can provide the minimum computer-use behavior Backseat needs on the current Windows machine.

This is an evidence-gathering milestone.

## Non-goals

- No production Backseat backend implementation.
- No AI provider integration.
- No GUI.
- No VM or isolated desktop implementation.
- No visual-novel-specific parser.
- No custom OCR.
- No native Win32 input implementation.

## Current state

Cua Driver 0.28.2 is installed and working. Tracked `opencode.json` remains Cua-disabled; no production backend wrapper exists yet.

Compatibility evidence is recorded for Calculator, Notepad, Doki Doki Literature Club Plus, and the OpenCode MCP harness under `experiments/cua-driver/`. The harness works in standard and bounded modes; bounded scope denies out-of-manifest tools and resources with structured errors.

Open items: production work proceeds under `docs/plans/active/0007-runtime-hardening.md` and `docs/plans/active/0008-mcp-server.md`; contracts, adapter, session, persistence, and the CLI host are completed in `docs/plans/completed/`. The first milestone is demonstrated end to end in `experiments/cua-driver/milestone-background-action.md`. `docs/decisions/0004-foreground-input-policy.md` is accepted.

The DDLC Plus process was left running because no driver path could close it; the human operator closed it manually (the game ignores background close attempts and `kill_app` refuses cross-transport provenance).

## Safety setup

Before giving Cua to an agent, read:

- `docs/runbooks/cua-agent-safety.md`
- `docs/runbooks/compatibility-testing.md`
- `docs/runbooks/trust-boundary.md`

Keep tracked `opencode.json` unchanged and Cua-disabled.

Use a local ignored runtime override through `scripts/opencode-cua.ps1`.

Prefer Cua bounded mode for unattended access.

If the initial experiment uses standard mode, keep it supervised, close sensitive applications, and record that the backend was desktop-wide.

## Questions

1. Can Cua Driver discover and target a specific application/window?
2. Can it capture that target while the user works elsewhere?
3. Can it deliver clicks without moving the user's physical cursor?
4. Can it type or press keys without stealing foreground focus?
5. What delivery routes are reported?
6. How are unsupported background actions surfaced?
7. How does behavior change for custom-rendered or game-like applications?
8. Can a visual novel remain rendered and interactive while unfocused?
9. What recording primitives are already usable?
10. Which failures would justify an isolated-session backend?

## Tasks

- [x] Run `pwsh ./scripts/doctor.ps1 -RequireCua`.

The Cua section passed. Core readiness still fails because the `opencode` CLI is not installed; that is an environment gap, not a Cua gap.
- [x] Install or verify Cua Driver.
- [x] Run `cua-driver mcp-config --client opencode`.

`mcp-config` generates the registration; the runtime command in tracked `opencode.json` is `cua-driver mcp`. Treat generated output as source of truth per `docs/runbooks/cua-agent-safety.md`.
- [x] Create `.backseat-local/opencode.cua.json` from the generated current registration.

Created with `mcp.cua` (renamed from the generated `cua-driver` key) and bounded-mode environment variables.
- [x] Decide whether the experiment uses bounded or supervised standard mode.

Decision: supervised standard mode for the first spike. Bounded mode needs a reviewed capability manifest that does not exist yet.
- [x] Configure and review the Cua capability boundary.

Completed: `version: 3` manifest with observation-only tools scoped to Notepad; bounded validation passed; out-of-scope calls fail closed.
- [x] Launch OpenCode through `pwsh ./scripts/opencode-cua.ps1`.

Verified in standard and bounded modes; the agent called `cua_list_windows` successfully. See `experiments/cua-driver/opencode-mcp-harness.md`.
- [x] Test a simple Windows application.

Calculator (WinUI) and Notepad (XAML).
- [x] Record whether cursor movement or foreground changes occur.
- [x] Test one real visual novel.

Doki Doki Literature Club Plus (Unity).
- [x] Write experiment results under `experiments/cua-driver/`.
- [x] Update `docs/research/cua-driver.md` with verified findings.
- [x] End the Cua-enabled OpenCode process when the experiment is complete.

No Cua-enabled OpenCode process was ever started on this machine (CLI-only path). The driver daemon remains running by design and is guarded by `autostart`.
- [x] Decide whether production wrapper work should begin.

Decided: yes, operator instructed. Production work proceeds under `docs/plans/active/0002-core-contracts.md`; the foreground-input policy is recorded in `docs/decisions/0004-foreground-input-policy.md`.

## Acceptance criteria

- [x] At least one ordinary Windows application has a documented compatibility result.
- [x] At least one visual novel has a documented compatibility result.
- [x] Every tested action notes whether the physical cursor moved.
- [x] Every tested action notes whether foreground focus changed.
- [x] The Cua permission mode used during the test is recorded.
- [x] Failures and unsupported behavior are documented.
- [x] Tracked `opencode.json` remains Cua-disabled.
- [x] The next implementation step is based on observed behavior, not assumption.

Cursor-movement evidence is honest but not conclusive: the desktop was in active human use, so driver-attributable movement is inferred from receipts rather than an idle-desktop measurement.

This plan is intentionally stricter than `docs/product/requirements.md`: the milestone needs one ordinary application, while this spike additionally requires one visual-novel result.

## Verification

Run:

```powershell
pwsh ./scripts/doctor.ps1 -RequireCua
pwsh ./scripts/verify.ps1
```

The canonical verifier should continue to pass because Cua enablement lives only in the local runtime override.

Any experiment code added during the spike should have a repeatable command documented in its local README.

## Discoveries

Evidence lives in `experiments/cua-driver/calculator.md`, `notepad.md`, `ddlc-plus.md`, and `opencode-mcp-harness.md`; the durable summary is in `docs/research/cua-driver.md`.

Question answers:

1. Yes. `list_windows` and `list_apps` provide pid, window id, bounds, and state; snapshots confirm exact identity.
2. Yes. Window screenshots returned live rendered frames while the target sat beneath four opaque full-screen windows (DDLC Plus at z=6). Caveat: virtual-desktop membership was not verified.
3. Yes for UIA-rich targets (Calculator 6x7=42 through background accessibility clicks, no foreground swap). No for the Unity visual novel, where background clicks were dropped.
4. XAML text input yes (`type_text` on Notepad returned `confirmed` with value read-back). Special keys are delivered but receipts are false-negative-prone: `press_key` reported `delivery_failed` while the key had actually landed.
5. Observed delivery modes: `background`, `foreground`. Routes: `accessibility`, `synthetic_events`, `global_input`. Effects: `confirmed`, `unverifiable`.
6. Not reliably. Failures either fail closed with explicit errors (minimized capture, documented `background_unavailable`) or return `unverifiable` while doing nothing (Unity). Receipts cannot be treated as success; re-observation is mandatory.
7. Custom-rendered targets expose no UIA content. Capture still works; background input is dropped; foreground escalation works.
8. Rendering continues while unfocused and covered. Interactivity while unfocused: no for this engine.
9. `start_recording` produced a valid H.264 1920x1080 30fps full-display mp4 plus `session.json` and `cursor.jsonl`. Per-turn trajectory folders require a persistent MCP client; a one-shot CLI finalizes the recording immediately.
10. An isolated-session backend becomes justified when a target drops background input and requires intrusive input, or when the human must keep using the machine while the agent works, or when recording must not capture the whole desktop.

Operational gaps found:

- `kill_app` refuses processes not provably launched by the same Cua runtime/transport. The polite UIA close worked on XAML apps but not on the Unity game, so no driver path could close it.
- Foreground escalation raised the target's z-order and did not restore it.
- `effect=unverifiable` must be treated as unknown; escalation hints can be false.
- Recording video captures the entire display, not just the target window.

Harness findings (see `experiments/cua-driver/opencode-mcp-harness.md`):

- The OpenCode MCP harness works in standard and bounded modes; an agent called `cua_list_windows` and received data.
- Bounded mode fails closed: `Permission denied: tool 'click' is outside the capability manifest` and `protected resource is outside the capability manifest` for wrong tools or resources.
- Cua's MCP tool schemas use non-standard `uint32`/`uint64` formats; OpenCode logs repeated ignorable warnings.
- `list_windows` requires `resources.desktop.display: true` in a bounded manifest.
- The launcher captured its first positional argument as `ConfigPath`, breaking pass-through subcommands; fixed in `scripts/opencode-cua.ps1`.

## Decisions made during implementation

- Used supervised standard mode for the CLI spike, then verified bounded mode with a reviewed observation-only manifest.
- Did not bypass any driver refusal (no OS-level termination of a refused process).
- Exercised foreground escalation once, only with explicit human approval, and prepared `docs/decisions/0004-foreground-input-policy.md` for the durable policy decision.
- Treated `unverifiable` receipts as unknown and verified every action by re-observation.
