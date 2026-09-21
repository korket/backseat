# Agent integration

How an agent connects to Backseat, and what the safety boundaries mean in practice.

Read `docs/runbooks/cua-agent-safety.md` and `docs/decisions/0004-foreground-input-policy.md` first. Backseat is a wrapper over Cua Driver, so the driver's authorization boundary still applies underneath.

## Surfaces

| Surface | Command | Use |
| --- | --- | --- |
| MCP (recommended) | `dotnet run --project src/Backseat.Mcp` | Agents drive Backseat through tools. |
| CLI | `dotnet run --project src/Backseat.Cli -- <verb>` | Humans and scripts run one-shot operations. |
| Library | `Backseat.Core` + `Backseat.Backends.Cua` | Embedding in another host. |

## MCP registration

The tracked `opencode.json` contains a disabled `backseat` server entry:

```json
{
  "mcp": {
    "backseat": {
      "type": "local",
      "command": ["dotnet", "run", "--project", "src/Backseat.Mcp"],
      "enabled": false
    }
  }
}
```

It ships disabled, like the Cua placeholder. Enable it deliberately when the workflow is ready: flip `enabled` to `true` in `opencode.json` (a privileged workflow file, so the change should be reviewed) and restart the client. `dotnet build Backseat.slnx` first, or add `--no-build` to the command to skip the build check on each connection.

For Claude Code and other MCP clients, register the same command with `--runs runs` to persist sessions.

## Server options

```text
backseat-mcp [--runs DIR] [--allow-foreground]
```

- `--runs DIR` writes each connection as a run under `DIR` (`metadata.json`, `actions.jsonl`, `events.jsonl`, opt-in screenshots). The run is finalized when the connection ends.
- `--allow-foreground` switches the delivery policy to `AllowForeground`. Without it the session is `BackgroundOnly` and any foreground receipt raises a violation that the tool reports as an error. Prefer the default; opt in only for attended work that the user has authorized.

## Tools

- `backseat_targets` - list discoverable targets (`processId`, `windowId`, `title`).
- `backseat_observe` - observe the selected target: structured elements with tokens, accessibility tree, optional PNG screenshot.
- `backseat_act` - execute one background action (`click`, `token`, `type`, `key`, `scroll`, `wait`) and return its receipt.

One connection is one Backseat session: the first `observe` or `act` selects the target, and later calls must use the same target. Start a new connection to switch.

## Reading receipts

- `effect=Confirmed` means the backend verified the result; `Unverifiable` means it delivered the action but could not verify it. Treat `Unverifiable` as unknown, not success, and re-observe before assuming the action landed.
- `delivery=Background` is the only value that may be described as background-safe, and only together with `effect=Confirmed` (`backgroundSafe=true`).
- `suggestedEscalation` is advisory. Never escalate automatically; see `docs/decisions/0004-foreground-input-policy.md`.
- Failed receipts carry the backend error text. Do not retry blindly; some failures are refusals that will not change.

## Recommended agent loop

1. `backseat_targets` and pick the exact target.
2. `backseat_observe` and address elements by `token` when available; pixel clicks are for custom-rendered surfaces.
3. `backseat_act` once per step.
4. `backseat_observe` again and verify the expected change before the next step.
5. On `Unverifiable` or degraded observations, re-observe (the session settles automatically) rather than escalating.

## Safety reminders

- Real desktop interaction must be human-initiated and supervised for experiments; see `docs/runbooks/compatibility-testing.md`.
- `kill_app` and close paths have provenance limits: a process launched by one invocation may refuse termination from another. Prefer polite closes through the target's own UI.
- Run artifacts can contain sensitive content, including screenshots of the whole desktop when backend recording is enabled.
