# Cua agent safety

Cua Driver is an external computer-use backend. Its authorization boundary is separate from Backseat's future target policy.

## Bootstrap default

The tracked `mcp.cua` entry in `opencode.json` stays disabled.

Do not modify the tracked project configuration just to run a compatibility experiment.

## Generate the current OpenCode registration

Before using Cua with OpenCode, run:

```powershell
cua-driver mcp-config --client opencode
```

Treat that generated output as the source of truth for the currently installed Cua command/path.

Create an ignored local runtime override at:

```text
.backseat-local/opencode.cua.json
```

The file must contain an `mcp.cua` configuration based on the generated current registration and must explicitly set:

```json
{
  "mcp": {
    "cua": {
      "enabled": true
    }
  }
}
```

Include the generated `type`, `command`, and any other required fields.

Do not copy stale command paths from documentation when the installed Cua version can generate its own registration.

## Prefer bounded mode

A standard Cua runtime can deliver input across desktop applications.

For unattended agent work on Windows/Linux, add bounded-mode environment variables to the local `mcp.cua` configuration:

```json
{
  "mcp": {
    "cua": {
      "environment": {
        "CUA_DRIVER_PERMISSION_MODE": "bounded",
        "CUA_DRIVER_CAPABILITY_MANIFEST_FILE": "C:\\absolute\\path\\to\\reviewed-manifest.yaml",
        "CUA_DRIVER_CAPABILITY_MANIFEST_APPROVED": "1"
      },
      "enabled": true
    }
  }
}
```

The example is incomplete by design: preserve the generated `type` and `command` from `cua-driver mcp-config --client opencode`.

The capability manifest path must be absolute and the manifest must be reviewed before launch.

## Launch without changing tracked config

Run:

```powershell
pwsh ./scripts/opencode-cua.ps1
```

The launcher:

1. reads `.backseat-local/opencode.cua.json`;
2. validates that Cua is explicitly enabled;
3. requires bounded mode by default;
4. injects the local config through `OPENCODE_CONFIG_CONTENT`;
5. launches OpenCode from the repository root;
6. restores the caller's prior environment afterward.

OpenCode's inline config has higher precedence than the tracked project config, so Cua becomes enabled only for that process.

The tracked `opencode.json` remains safe and verifiable.

## Supervised standard-mode experiment

If bounded mode is not practical for the first compatibility spike, you may explicitly run:

```powershell
pwsh ./scripts/opencode-cua.ps1 -AllowStandardMode
```

Only do this for a supervised experiment.

Before doing so:

1. close applications containing sensitive information;
2. understand that standard mode can reach applications across the desktop;
3. record in the experiment notes that the backend was desktop-wide;
4. stop the session when the test is complete.
5. record the cursor, focus, and delivery-route receipt for every action; never log standard-mode success as background-safe evidence.

Do not describe standard-mode testing as proving Backseat's final safety boundary.

## Readiness

Before a Cua-required task, run:

```powershell
pwsh ./scripts/doctor.ps1 -RequireCua
```

The doctor verifies the installed binary, current diagnostics, and that Cua can generate its OpenCode MCP configuration.

On Windows/Linux, the bare `cua-driver mcp` process can own its own runtime, so a persistent daemon is useful but not mandatory.

Actual desktop access must still be verified by the compatibility experiment.

## Backseat invariant

Backseat's future production session policy must remain explicit even if an underlying backend is more permissive.

A permissive backend is not permission for the agent to roam across the desktop.
