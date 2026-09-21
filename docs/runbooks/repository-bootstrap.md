# Repository bootstrap

Use this once when Backseat is first copied into a fresh repository.

## Fresh directory with no Git repository

Run:

```powershell
pwsh ./scripts/bootstrap.ps1 -InitializeGit -CommitBootstrap
```

The script stages only the exact files listed in `BOOTSTRAP-FILES.txt`; it does not bulk-stage repository directories.

The script:

1. checks Git identity;
2. initializes Git with `main` when needed;
3. stages only known bootstrap paths;
4. creates the initial commit:

```text
build: bootstrap Backseat repository
```

It refuses to create a bootstrap commit when repository history already exists.

## Existing Git repository

Do not run the automatic bootstrap commit against an existing history.

Instead:

1. copy the bootstrap files into the repository;
2. inspect the diff;
3. commit the bootstrap manually as appropriate for that repository;
4. run:

```powershell
pwsh ./scripts/doctor.ps1
```

## After the bootstrap commit

All product implementation work must use topic/fix branches.

The coding agent may create those branches and local commits automatically. It may push only with explicit human approval for that push; merging into `main` happens only when the human explicitly instructs it.

## SDK selection

The bootstrap includes `global.json`, which keeps repository `dotnet` commands on the .NET 10 SDK line.

## Local runtime state

`.backseat-local/` is intentionally ignored and is used for machine-specific runtime overrides such as Cua-enabled OpenCode configuration.
