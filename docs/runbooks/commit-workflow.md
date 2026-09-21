# Commit workflow

Backseat uses small, reviewable commits as part of normal agent execution.

The coding agent is expected to create local commits automatically as coherent work is completed.

## Goals

The commit history should:

- tell a clean technical story;
- preserve useful checkpoints;
- keep behavior changes reviewable;
- make regressions easier to locate;
- avoid one giant commit at the end of a task.

## Bootstrap exception

A brand-new repository needs one base commit before topic-branch work begins.

The bootstrap procedure may create this one commit directly on `main`:

```text
build: bootstrap Backseat repository
```

After that commit exists, product work must happen on a topic or fix branch.

See `docs/runbooks/repository-bootstrap.md`.

## Branch model

Use `main` as the human-controlled integration branch.

Implementation work happens on short-lived branches such as:

```text
topic/<area>/<feature>
fix/<area>/<bug>
```

Examples:

```text
topic/core/session-model
topic/cua/action-receipts
fix/cua/cursor-reporting
```

If an agent begins a task while already on an existing non-main work branch, it should normally continue there.

## Agent ownership

The build agent owns:

- local implementation;
- relevant tests;
- commit boundaries;
- local commits;
- reporting the final commit series.

The human owns:

- deciding when work merges into `main`;
- pushing to remotes;
- rebasing or otherwise rewriting history;
- destructive Git operations;
- publication or release decisions.

A merge into `main` happens only when the human explicitly instructs it. The agent then performs the merge locally; publishing still requires the human.

## Privileged workflow files

The Git helper scripts and OpenCode policy are themselves part of the trust mechanism.

OpenCode requires human approval before editing `scripts/*.ps1`, `opencode.json`, `AGENTS.md`, `.gitignore`, `global.json`, or `BOOTSTRAP-FILES.txt`.

An agent must not bypass that approval requirement through shell commands or generated code.

## OpenCode mode

Do not use OpenCode `--auto` for normal Backseat development.

Unknown shell commands intentionally require approval. Known development, verification, staging, and commit commands are explicitly allowed in `opencode.json`.

## Patch discipline

Each commit should represent one coherent change.

Good:

```text
core: define action delivery metadata
cua: map backend delivery details into receipts
cli: display background safety information
```

Bad:

```text
implement feature
fix tests
oops
cleanup
final fix
```

Use a component area (`cua: ...`, `docs: ...`, `scripts: ...`); never mirror the branch kind into the subject (`fix: ...`, `wip: ...`). `scripts/commit.ps1` rejects those subjects.

## Tests travel with behavior

When a commit changes behavior, include the tests for that behavior in the same commit where practical.

Avoid patterns where production code lands in one commit and its tests appear much later unless the sequence itself is intentionally test-driven and remains buildable.

## Staging

Autonomous agents must not call `git add` directly.

Use:

```powershell
pwsh ./scripts/stage.ps1 -Paths src/Backseat.Core/File.cs tests/Backseat.Core.Tests/FileTests.cs
```

The helper rejects:

- staging on `main`/`master`;
- repository-wide `.` staging;
- wildcards;
- parent traversal;
- absolute paths.

This turns explicit staging into an executable invariant rather than a prompt convention.

## Committing

Autonomous agents must not call `git commit` directly.

Use:

```powershell
pwsh ./scripts/commit.ps1 `
  -Subject "core: preserve delivery metadata" `
  -Body "Explain why the invariant is required and any non-obvious consequence."
```

The helper rejects:

- commits on `main`/`master`;
- detached/unborn branches;
- merge/rebase/cherry-pick/revert states;
- malformed or throwaway subjects;
- empty staged sets;
- common generated/runtime output;
- staged diff errors detected by `git diff --staged --check`.

## Merging

Merge into `main` only when the human explicitly instructs it.

- Merge only completed, verified work; run the full verification first.
- Prefer `git merge --ff-only` when the branch is linear; it keeps history readable and fails loudly instead of inventing a merge commit.
- Never rewrite `main` history, force-push, or merge unfinished work.
- Report the merged commit range after the merge.
- Do not delete the source branch unless the human asks.

## During a larger task

Before coding, the agent should identify the intended series.

Example:

```text
[1/4] core: define backend capability model
[2/4] cua: add target discovery adapter
[3/4] cua: preserve action delivery receipts
[4/4] cli: expose target observation
```

Then work progressively:

```text
implement slice
    ↓
verify slice
    ↓
stage explicit paths
    ↓
commit through helper
    ↓
next slice
```

Do not intentionally leave all commits until the end.

## Mistakes and cleanup

Normal build agents are intentionally not granted autonomous rebase, reset, amend, cherry-pick, or push permissions.

Merging is permitted only on explicit human instruction; it is never an autonomous cleanup step.

If a local mistake is discovered during implementation:

- make the smallest correct follow-up commit;
- report that cleanup may be desirable before merge;
- let the human explicitly perform or authorize history cleanup.

Never rewrite shared `main` history.

## End-of-task checklist

```text
[ ] full verification passes
[ ] working tree is clean
[ ] no unrelated files were committed
[ ] commit sequence is understandable
[ ] commit messages explain motivation
[ ] no WIP/oops/debug commits remain
[ ] commits created are reported
[ ] nothing was pushed or merged automatically
```
