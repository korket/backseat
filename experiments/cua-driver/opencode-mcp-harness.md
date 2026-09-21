# OpenCode MCP harness and bounded-scope verification

Supervised test of the OpenCode-to-Cua MCP path, first in standard mode, then in bounded mode with a reviewed capability manifest.

```text
Application: Windows Notepad (packaged app) as the allowed observation target
Backend: Cua Driver
Backend version: cua-driver 0.28.2 (x86_64-windows)
Client: OpenCode 1.18.31 (npm install -g opencode-ai)
PermissionMode: standard (first run), bounded (second run)
CapabilityBoundary: .backseat-local/cua-capabilities.yaml (machine-local, ignored)
```

## Setup

- Local override `.backseat-local/opencode.cua.json` generated from `cua-driver mcp-config --client opencode`, renamed to `mcp.cua`, and validated by `scripts/opencode-cua.ps1`.
- Standard-mode run used `-AllowStandardMode`; bounded-mode run used the manifest environment variables and passed launcher validation without the flag.

## Standard-mode run

- Prompt asked the agent to call the window-listing tool once.
- The agent invoked `cua_list_windows` and answered `12`.
- The server registered correctly; OpenCode emitted repeated `unknown format "uint32"/"uint64" ignored in schema` warnings from Cua's MCP tool schemas. Noisy but harmless.
- The default configured model failed with `Model access is disabled`; the run succeeded with `-m opencode-go/deepseek-v4.1-flash`.

## Bounded-mode run

Manifest (`version: 3`, `expires_after: 2h`, `idle_timeout: 20m`):

- `allow.tools`: `start_session`, `end_session`, `launch_app`, `list_windows`, `get_window_state`
- `resources.apps`: Notepad executable, `launch: true`, `windows: all`, `terminate: deny`
- `resources.desktop.display`: `true` (required: discovery crosses the desktop)

Results:

- In scope: `cua_list_windows` returned 12 windows; `cua_get_window_state` returned the Notepad title.
- Out of scope: `cua_click` returned `Permission denied: tool 'click' is outside the capability manifest`.

Fail-closed errors observed while tuning the manifest:

- `protected resource is outside the capability manifest: desktop display observation is outside the capability manifest` (before `display: true`).
- `protected resource is outside the capability manifest: desktop pid 26276 window 723780 is outside the capability manifest` (wrong executable path).

Manifest authoring notes:

- On Windows the application identity is a canonical absolute executable path. For packaged apps this is under `WindowsApps`, including inner subdirectories (Notepad's real image is `<package>\Notepad\Notepad.exe`, not `<package>\Notepad.exe`).
- Allowed tools alone are not sufficient; each call's resources must also match.
- `list_windows` needs `desktop.display: true`; `get_window_state` alone does not.

## Harness findings

- The launcher's first positional parameter used to capture the subcommand (for example `run`) as `ConfigPath`; fixed so pass-through arguments bind correctly.
- Recording remains transport-scoped: trajectory recording requires a persistent MCP client, which the OpenCode server process provides for the life of a session.

## Conclusion

The OpenCode MCP harness works end-to-end in both modes. Bounded mode delivers the exact Backseat requirement: in-scope calls run silently, out-of-scope tools and resources fail closed with structured errors, and no input tool is reachable unless the manifest allows it.
