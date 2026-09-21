param(
    [string]$ConfigPath = ".backseat-local/opencode.cua.json",

    [switch]$AllowStandardMode,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$OpenCodeArgs
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $RepoRoot

try {
    if ($ConfigPath -eq "--auto" -or $OpenCodeArgs -contains "--auto") {
        throw "OpenCode --auto is not allowed for Backseat development. See AGENTS.md."
    }

    $resolvedConfig = if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
        $ConfigPath
    }
    else {
        Join-Path $RepoRoot $ConfigPath
    }

    if (-not (Test-Path -LiteralPath $resolvedConfig -PathType Leaf)) {
        throw @"
Local Cua OpenCode override not found:
  $resolvedConfig

Generate the current Cua OpenCode registration first:

  cua-driver mcp-config --client opencode

Then create .backseat-local/opencode.cua.json as described in:
  docs/runbooks/cua-agent-safety.md
"@
    }

    $raw = Get-Content -LiteralPath $resolvedConfig -Raw

    try {
        $localConfig = $raw | ConvertFrom-Json
    }
    catch {
        throw "Local Cua override is not valid JSON: $($_.Exception.Message)"
    }

    if ($null -eq $localConfig.mcp -or $null -eq $localConfig.mcp.cua) {
        throw "Local override must define mcp.cua."
    }

    $topLevelKeys = @($localConfig.PSObject.Properties.Name)
    $unexpectedTopLevel = @($topLevelKeys | Where-Object { $_ -ne 'mcp' })
    if ($unexpectedTopLevel.Count -gt 0) {
        throw "Local override must contain only the 'mcp' section. Unexpected top-level key(s): $($unexpectedTopLevel -join ', ')."
    }

    if ($null -ne $localConfig.permission) {
        throw "Local override must not define 'permission'. Tracked opencode.json guardrails stay in effect."
    }

    if ($localConfig.mcp.cua.enabled -ne $true) {
        throw "Local override must explicitly set mcp.cua.enabled to true."
    }

    $environment = $localConfig.mcp.cua.environment
    $permissionMode = $null
    if ($null -ne $environment) {
        $permissionMode = $environment.CUA_DRIVER_PERMISSION_MODE
    }

    if (-not $AllowStandardMode) {
        if ($permissionMode -ne "bounded") {
            throw @"
The local Cua override is not configured for bounded mode.

For unattended use, set:
  CUA_DRIVER_PERMISSION_MODE=bounded
  CUA_DRIVER_CAPABILITY_MANIFEST_FILE=<reviewed absolute manifest path>
  CUA_DRIVER_CAPABILITY_MANIFEST_APPROVED=1

For a supervised compatibility experiment only, rerun this launcher with
-AllowStandardMode after following docs/runbooks/cua-agent-safety.md.
"@
        }

        if ([string]::IsNullOrWhiteSpace($environment.CUA_DRIVER_CAPABILITY_MANIFEST_FILE)) {
            throw "Bounded mode requires CUA_DRIVER_CAPABILITY_MANIFEST_FILE."
        }

        if ("$($environment.CUA_DRIVER_CAPABILITY_MANIFEST_APPROVED)" -ne "1") {
            throw "Bounded mode requires CUA_DRIVER_CAPABILITY_MANIFEST_APPROVED=1."
        }
    }
    else {
        Write-Host "[WARN] Launching Cua experiment without enforcing bounded mode."
        Write-Host "[WARN] This is for supervised compatibility testing only."
    }

    $previousInlineConfig = $env:OPENCODE_CONFIG_CONTENT

    try {
        # Inline config has higher precedence than the tracked project config.
        $env:OPENCODE_CONFIG_CONTENT = $raw

        Write-Host "Launching OpenCode with local Cua runtime override:"
        Write-Host "  $resolvedConfig"
        Write-Host ""
        & opencode @OpenCodeArgs
        $exitCode = $LASTEXITCODE
    }
    finally {
        if ($null -eq $previousInlineConfig) {
            Remove-Item Env:OPENCODE_CONFIG_CONTENT -ErrorAction SilentlyContinue
        }
        else {
            $env:OPENCODE_CONFIG_CONTENT = $previousInlineConfig
        }
    }

    exit $exitCode
}
finally {
    Pop-Location
}
