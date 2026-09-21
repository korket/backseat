# Development runbook

Read `docs/runbooks/toolchain.md` and `docs/runbooks/trust-boundary.md` before the first development session.

For a brand-new repository, complete `docs/runbooks/repository-bootstrap.md` first.

## Start a work session

Use normal OpenCode mode, not `--auto`.

1. Read `AGENTS.md`.
2. Read the active plan relevant to the task.
3. Run:

```powershell
pwsh ./scripts/doctor.ps1
```

4. Inspect architecture and ADRs that affect the task.
5. If platform behavior is uncertain, create or update an experiment before production code.
6. Before giving an agent desktop access through Cua, read `docs/runbooks/cua-agent-safety.md`.

## During implementation

Follow `docs/runbooks/commit-workflow.md` for branch and autonomous commit behavior.

- Keep changes inside the current plan.
- Prefer the smallest vertical slice that produces evidence.
- Update the plan when discoveries invalidate assumptions.
- Add tests for deterministic behavior.
- Keep real desktop interaction outside the normal unit test path.

## Before finishing

Run:

```powershell
pwsh ./scripts/verify.ps1
```

Then:

- update the active plan;
- record unresolved assumptions;
- create an ADR for durable architectural changes;
- move a finished plan to `docs/plans/completed/`.
