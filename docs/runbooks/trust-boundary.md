# Trust boundary

OpenCode permissions and Backseat's Git helper scripts are workflow guardrails.

They are **not** a hostile-code security sandbox.

## What the repository guardrails are for

The tracked OpenCode policy is designed to reduce accidental or routine agent mistakes:

- unknown shell commands require approval;
- destructive Git operations and history rewriting are denied;
- publishing requires explicit human approval for each push;
- merges into `main` happen only on explicit human instruction;
- normal autonomous commits go through guarded helper scripts;
- privileged workflow files require approval before editing;
- Cua is disabled in the tracked configuration.

These controls make ordinary agent behavior predictable and auditable.

## What they do not protect against

An agent that can edit project source and run builds/tests can cause project-controlled code to execute.

For example, build systems, test runners, generators, package scripts, source generators, native helpers, or intentionally modified project code can execute with the host user's authority.

Therefore:

```text
OpenCode permissions + repository helpers
    = workflow safety and accident resistance

restricted OS user / sandbox / VM / other isolation
    = security boundary against untrusted or adversarial code
```

Do not treat a shell allowlist as containment.

## Protected workflow files

The tracked OpenCode configuration requires approval before editing:

- `AGENTS.md`
- `opencode.json`
- `.gitignore`
- `global.json`
- `BOOTSTRAP-FILES.txt`
- `scripts/*.ps1`

This prevents the normal build agent from silently weakening the same helpers it is allowed to execute.

A human may deliberately approve changes to these files when the workflow itself needs maintenance.

## External dependencies

Treat code downloaded from packages, repositories, generated patches, or agent-created build scripts as executable code.

Review dependency changes and scripts according to the risk of the environment where they will run.

## Cua

Cua can directly interact with desktop applications.

The tracked OpenCode configuration keeps it disabled.

For unattended use, prefer Cua bounded mode with a reviewed capability manifest. A permissive Cua runtime is not equivalent to a Backseat target authorization boundary.

See `docs/runbooks/cua-agent-safety.md`.
