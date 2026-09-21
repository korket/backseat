# Plan: MCP server

Status: Active

## Goal

Expose Backseat to MCP clients (OpenCode, Claude Code, and similar agents) through a stdio JSON-RPC server so an agent can discover targets, observe them, and act with receipts, all through Backseat's session, policy, and run persistence.

## Non-goals

- No HTTP/SSE transport; stdio only.
- No resources or prompts; tools only.
- No multi-target session switching; one connection is one Backseat session.
- No dependency on an external MCP SDK; the protocol surface needed here is small and hand-rolled.

## Current state

The CLI host, session, adapter, and persistence are merged (plans 0002-0006 completed). No agent-facing surface exists yet.

## Unknowns

- Which MCP protocol revision the clients in use negotiate; the server echoes the client's requested revision and defaults to `2024-11-05`.
- Whether agents need screenshots inline; the tool supports an opt-in base64 image block.

## Tasks

- [x] Add the `Backseat.Mcp` project with a testable stdio JSON-RPC loop.
- [x] Implement `initialize`, `tools/list`, `tools/call`, `ping`, and notification handling.
- [x] Expose `backseat_targets`, `backseat_observe`, and `backseat_act` backed by one session per connection.
- [x] Support `--runs DIR` and `--allow-foreground` server options.
- [x] Cover the protocol and tools with in-memory tests.

## Acceptance criteria

- [x] `initialize` returns the negotiated protocol version and server info.
- [x] `tools/list` advertises the three tools with input schemas.
- [x] `tools/call` dispatches to discovery, observation, and action execution, returning JSON text content.
- [x] Unknown methods return JSON-RPC error `-32601`; malformed JSON returns `-32700`.
- [x] A second target on the same connection is refused with an actionable message.
- [x] The server closes the session and finalizes the run when stdin ends.
- [x] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

Result: build clean, 115 tests pass.

## Discoveries

- Live smoke over stdio: initialize negotiated `2025-06-18`, `backseat_targets` returned real targets, `backseat_observe` returned the OpenCode window observation, `backseat_act` returned a wait receipt, and the run was finalized as `Closed` with one action when stdin ended.

## Decisions made during implementation

- One connection is one Backseat session: the first observe or act selects the target and later calls must match it, which mirrors the single-target session model.
- No external MCP SDK; the needed protocol surface is small, so it is hand-rolled and fully unit-tested.
- Notifications never receive responses, per JSON-RPC.
