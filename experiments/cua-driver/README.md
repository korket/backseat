# Cua Driver compatibility experiments

Use this directory for real application compatibility notes.

Suggested files:

```text
notepad.md
calculator.md
<visual-novel-name>.md
```

Follow `docs/runbooks/compatibility-testing.md` and `docs/runbooks/cua-agent-safety.md`.

Default to bounded mode through `scripts/opencode-cua.ps1`. Use `-AllowStandardMode` for supervised compatibility testing only, with sensitive applications closed.

Visual-novel targets here are compatibility targets only, not core-runtime behavior.
