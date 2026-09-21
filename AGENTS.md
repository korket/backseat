# Backseat agent guide

Backseat is a Windows-first computer-use runtime for AI agents.

## Read first

Product:
- `docs/product/vision.md`
- `docs/product/requirements.md`
- `docs/product/non-goals.md`

Architecture:
- `ARCHITECTURE.md`
- `docs/design/index.md`

Current work:
- `docs/plans/active/`

Research:
- `docs/research/`

Toolchain and trust:
- `docs/runbooks/toolchain.md`
- `docs/runbooks/trust-boundary.md`

## Core rules

- Keep AI reasoning separate from computer execution.
- Keep external computer-use backends behind Backseat-owned interfaces.
- Never silently fall back from background-safe input to global mouse or keyboard injection.
- Never claim an action was background-safe unless the backend confirms it.
- Target application/process/window boundaries must be explicit.
- Real desktop interaction tests must be opt-in.
- Unit tests must not click, type into, or focus arbitrary user applications.
- Put uncertain Windows behavior in `experiments/` before designing production architecture around it.
- Prefer small vertical slices over speculative frameworks.
- Update docs or ADRs when implementation changes an established contract.
- Do not add visual-novel-specific behavior to the core runtime.
- Do not run OpenCode with `--auto`; this repository intentionally keeps unknown shell commands behind approval.
- Treat OpenCode permissions as workflow guardrails, not a security sandbox.
- Do not autonomously modify privileged workflow files when OpenCode requests approval.

## Privileged workflow files

Changes to these require explicit human approval:

- `AGENTS.md`
- `opencode.json`
- `.gitignore`
- `global.json`
- `BOOTSTRAP-FILES.txt`
- `scripts/*.ps1`

Do not work around these edit protections through shell commands, build scripts, generated code, or another tool.

## Git and commit workflow

The build agent is expected to commit its own completed work locally.

The one-time repository bootstrap commit may be created on `main` by the bootstrap procedure. After that bootstrap, never implement product work directly on `main`.

### Branches

Before modifying product code:

1. inspect the current branch;
2. if currently on `main`, create a short-lived topic branch;
3. prefer names such as:
   - `topic/<area>/<feature>`
   - `fix/<area>/<bug>`;
4. do not switch away from an existing non-main work branch unless the task explicitly requires it.

### Commit discipline

Treat commits as reviewable patches.

- One logical change per commit.
- Commit as soon as one coherent slice is implemented and verified.
- Tests belong in the same commit as the behavior they verify.
- Do not accumulate an entire task into one large final commit.
- Do not commit broken intermediate states.
- Keep each commit buildable and testable where practical.
- Do not mix unrelated formatting, refactoring, or cleanup.
- Commit messages must explain why the change exists.
- Do not create `WIP`, `fix`, `oops`, or similar throwaway commits.
- Do not call `git add` or `git commit` directly during autonomous work.
- Stage explicit file paths through `scripts/stage.ps1`.
- Commit through `scripts/commit.ps1`.
- Do not amend, rebase, merge, cherry-pick, reset, clean, push, or force-push automatically.

### Before each commit

1. inspect `git status`;
2. inspect staged and unstaged diffs;
3. run tests appropriate to that logical change;
4. stage only files belonging to that change:

```powershell
pwsh ./scripts/stage.ps1 -Paths path/to/file1 path/to/file2
```

5. inspect the staged diff;
6. commit automatically:

```powershell
pwsh ./scripts/commit.ps1 `
  -Subject "area: imperative summary" `
  -Body "Explain why the change is needed and any important constraints."
```

### End of task

Before declaring a task complete:

1. run the full verification command;
2. inspect `git status`;
3. inspect the complete commit series against its base;
4. report the commits created;
5. leave the working tree clean.

Do not push or merge into `main`.

For the full workflow, read `docs/runbooks/commit-workflow.md`.

## Canonical commands

Environment check:

```powershell
pwsh ./scripts/doctor.ps1
```

When Cua is required for the compatibility task:

```powershell
pwsh ./scripts/doctor.ps1 -RequireCua
```

Launch a Cua-enabled OpenCode process using only a local ignored override:

```powershell
pwsh ./scripts/opencode-cua.ps1
```

Full verification:

```powershell
pwsh ./scripts/verify.ps1
```

## Before implementation

For non-trivial work:

1. read the relevant active plan;
2. inspect applicable ADRs and design docs;
3. identify unknown platform behavior;
4. create or update an experiment when evidence is missing;
5. define acceptance criteria before expanding scope.

## Before finishing

- run `pwsh ./scripts/verify.ps1`;
- document unresolved assumptions;
- update the active plan with discoveries;
- add an ADR if a durable architectural decision was made.
