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

- [ ] Run `pwsh ./scripts/doctor.ps1 -RequireCua`.
- [ ] Install or verify Cua Driver.
- [ ] Run `cua-driver mcp-config --client opencode`.
- [ ] Create `.backseat-local/opencode.cua.json` from the generated current registration.
- [ ] Decide whether the experiment uses bounded or supervised standard mode.
- [ ] Configure and review the Cua capability boundary.
- [ ] Launch OpenCode through `pwsh ./scripts/opencode-cua.ps1`.
- [ ] Test a simple Windows application.
- [ ] Record whether cursor movement or foreground changes occur.
- [ ] Test one real visual novel.
- [ ] Write experiment results under `experiments/cua-driver/`.
- [ ] Update `docs/research/cua-driver.md` with verified findings.
- [ ] End the Cua-enabled OpenCode process when the experiment is complete.
- [ ] Decide whether production wrapper work should begin.

## Acceptance criteria

- [ ] At least one ordinary Windows application has a documented compatibility result.
- [ ] At least one visual novel has a documented compatibility result.
- [ ] Every tested action notes whether the physical cursor moved.
- [ ] Every tested action notes whether foreground focus changed.
- [ ] The Cua permission mode used during the test is recorded.
- [ ] Failures and unsupported behavior are documented.
- [ ] Tracked `opencode.json` remains Cua-disabled.
- [ ] The next implementation step is based on observed behavior, not assumption.

## Verification

Run:

```powershell
pwsh ./scripts/doctor.ps1 -RequireCua
pwsh ./scripts/verify.ps1
```

The canonical verifier should continue to pass because Cua enablement lives only in the local runtime override.

Any experiment code added during the spike should have a repeatable command documented in its local README.

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
