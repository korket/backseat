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

- [ ] Add the `Backseat.Mcp` project with a testable stdio JSON-RPC loop.
- [ ] Implement `initialize`, `tools/list`, `tools/call`, `ping`, and notification handling.
- [ ] Expose `backseat_targets`, `backseat_observe`, and `backseat_act` backed by one session per connection.
- [ ] Support `--runs DIR` and `--allow-foreground` server options.
- [ ] Cover the protocol and tools with in-memory tests.

## Acceptance criteria

- [ ] `initialize` returns the negotiated protocol version and server info.
- [ ] `tools/list` advertises the three tools with input schemas.
- [ ] `tools/call` dispatches to discovery, observation, and action execution, returning JSON text content.
- [ ] Unknown methods return JSON-RPC error `-32601`; malformed JSON returns `-32700`.
- [ ] A second target on the same connection is refused with an actionable message.
- [ ] The server closes the session and finalizes the run when stdin ends.
- [ ] `dotnet test` and `pwsh ./scripts/verify.ps1` pass.

## Verification

```powershell
pwsh ./scripts/verify.ps1
```

## Discoveries

Add findings here as work progresses.

## Decisions made during implementation

Promote durable architecture changes to `docs/decisions/`.
