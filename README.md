# Backseat

Backseat is a Windows-first computer-use runtime for AI agents.

The goal is to let an agent observe and operate a selected desktop application while the human continues using the same computer.

Visual novels are the first real-world workload, but Backseat is intentionally application-agnostic.

## Current phase

Backseat is in the compatibility and architecture validation phase.

The first milestone is not a full autonomous agent platform. It is to prove that we can:

1. identify and target one real Windows application;
2. observe that target reliably;
3. deliver background-safe input when the backend supports it;
4. report when an action would require foreground or global input;
5. record observations, actions, and results;
6. keep the user's normal desktop usable during the session.

## Repository map

- `AGENTS.md` - short instructions for coding agents.
- `ARCHITECTURE.md` - stable system boundaries and architecture map.
- `docs/product/` - product intent, requirements, terminology, and non-goals.
- `docs/design/` - deeper design documents.
- `docs/decisions/` - architecture decision records.
- `docs/plans/` - active and completed execution plans.
- `docs/research/` - durable research notes.
- `docs/runbooks/` - repeatable development, commit, and testing procedures.
- `src/` - production projects (`Backseat.Core`, `Backseat.Backends.Cua`, `Backseat.Cli`).
- `tests/` - unit tests for the production projects.
- `experiments/` - disposable technical experiments.
- `evals/` - behavior-level scenarios and fixtures.
- `scripts/` - canonical project commands.
- `runs/` - local runtime artifacts. Not committed.

## Development principle

Backseat separates agent reasoning from computer execution.

An AI model may decide what action it wants to perform, but the runtime is responsible for target boundaries, action delivery, receipts, recording, cancellation, and safety constraints.

## The `backseat` host

The first entry point is a CLI that drives the Cua backend through Backseat's own runtime:

```powershell
dotnet run --project src/Backseat.Cli -- targets
dotnet run --project src/Backseat.Cli -- observe --pid <pid> --window <window-id> --elements
dotnet run --project src/Backseat.Cli -- act --pid <pid> --window <window-id> --wait 100 --runs runs
```

`act` performs exactly one action, prints the receipt, and exits non-zero on failure. All input uses background delivery; the CLI never escalates to foreground.

## The MCP server

Agents drive Backseat over stdio MCP:

```json
{
  "mcp": {
    "backseat": {
      "type": "local",
      "command": ["dotnet", "run", "--project", "src/Backseat.Mcp"],
      "enabled": true
    }
  }
}
```

Tools: `targets`, `observe`, and `act` (namespaced by the client as `backseat_targets` and so on). One connection is one Backseat session: the first observe or act selects the target, and `--runs DIR` persists the connection as a run. The default delivery policy is background-only; `--allow-foreground` opts into intrusive escalation with receipts. The tracked `opencode.json` enables the server and prompts for approval on `backseat_act`.

See `docs/runbooks/agent-integration.md` for registration, receipt semantics, and the recommended agent loop.

## Initial backend strategy

The first compatibility work should evaluate an existing computer-use backend such as Cua Driver before Backseat implements native Windows automation.

Existing backends must remain behind Backseat-owned interfaces so they can be replaced later.


## Getting started

For a brand-new local repository after extracting this pack:

```powershell
pwsh ./scripts/bootstrap.ps1 -InitializeGit -CommitBootstrap
pwsh ./scripts/doctor.ps1
pwsh ./scripts/verify.ps1
```

If Git history already exists, do not use the automatic bootstrap commit. Follow `docs/runbooks/repository-bootstrap.md`.

Then read, in order:

1. `AGENTS.md`
2. `docs/product/vision.md`
3. `docs/product/requirements.md`
4. `docs/product/terminology.md`
5. `docs/product/non-goals.md`
6. `ARCHITECTURE.md`
7. `docs/plans/active/0001-cua-compatibility.md`

Cua Driver is deliberately disabled in `opencode.json` until its permission boundary is configured for the compatibility experiment. Cua experiments use an ignored local override launched through `scripts/opencode-cua.ps1`, so the tracked configuration stays safe by default.

Do not run OpenCode with `--auto` for Backseat development. The repository deliberately uses approval for unknown shell commands while explicitly allowing its known autonomous workflow. Read `docs/runbooks/cua-agent-safety.md` before enabling it.

## Trust model

OpenCode permissions and Backseat's Git helpers are workflow guardrails, not a hostile-code sandbox. Build/test execution can run project-controlled code with the host user's authority. See `docs/runbooks/trust-boundary.md`.

## Status


Pre-alpha. Expect architecture and implementation details to change as experiments produce evidence.
