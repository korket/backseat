# Toolchain

Backseat's bootstrap currently assumes the following local development toolchain.

## Required

### Git

Used for the patch-oriented autonomous commit workflow.

Git 2.28 or later is required for `git init -b main` in `scripts/bootstrap.ps1`.

The agent may create local topic branches and local commits through Backseat's guarded scripts. Publishing and history rewriting remain human-controlled.

### PowerShell 7 (`pwsh`)

Canonical repository scripts use PowerShell 7.

Scripts anchor themselves to the repository root, so they may be invoked from another working directory.

### .NET SDK 10

Backseat is currently a Windows-first .NET 10 project.

The repository includes `global.json`:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

This selects an installed .NET 10 feature band/patch rather than accidentally switching to a later major SDK.

`doctor.ps1` checks that a 10.x SDK is installed and that the repository-selected SDK is 10.x.

See `docs/decisions/0003-dotnet-10.md`.

### OpenCode

This bootstrap was reviewed against OpenCode `1.18.31`.

The repository uses the live schema referenced by:

```text
https://opencode.ai/config.json
```

`doctor.ps1` prints the installed OpenCode version and warns when it differs from the reviewed version.

A version mismatch is not automatically a failure because newer versions may remain compatible. If permission behavior changes, compare `opencode.json` against the live schema before continuing.

## Optional until required

### Cua Driver

Cua Driver is required for the first desktop compatibility experiment, but not for documentation-only/bootstrap work.

Run:

```powershell
pwsh ./scripts/doctor.ps1 -RequireCua
```

when working on that experiment.

## OpenCode mode

Do not run Backseat development sessions with OpenCode `--auto`.

Backseat deliberately uses approval for unknown shell commands while explicitly allowing known safe development commands. Auto mode weakens that approval boundary.

Use normal OpenCode mode. The commands intended to be autonomous are already permitted by `opencode.json`.

## Trust model

OpenCode permissions are workflow guardrails, not an adversarial security sandbox.

Read `docs/runbooks/trust-boundary.md`.
