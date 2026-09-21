$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $RepoRoot

try {
    Write-Host "Backseat verification"
    Write-Host "====================="

    $failed = $false

    function Fail {
        param([Parameter(Mandatory = $true)][string]$Message)
        Write-Host "[FAIL] $Message"
        $script:failed = $true
    }

    $required = @(
        "AGENTS.md",
        "ARCHITECTURE.md",
        "README.md",
        "opencode.json",
        "global.json",
        "BOOTSTRAP-FILES.txt",
        "docs/product/vision.md",
        "docs/product/requirements.md",
        "docs/product/non-goals.md",
        "docs/runbooks/commit-workflow.md",
        "docs/runbooks/repository-bootstrap.md",
        "docs/runbooks/cua-agent-safety.md",
        "docs/runbooks/toolchain.md",
        "docs/runbooks/trust-boundary.md",
        "docs/plans/active/0001-cua-compatibility.md",
        "scripts/stage.ps1",
        "scripts/commit.ps1",
        "scripts/opencode-cua.ps1"
    )

    foreach ($path in $required) {
        if (-not (Test-Path $path)) {
            Fail "Missing required bootstrap file: $path"
        }
    }

    if (Test-Path "opencode.json") {
        try {
            $config = Get-Content "opencode.json" -Raw | ConvertFrom-Json

            if ($null -eq $config.permission) {
                Fail "opencode.json must use the live schema's top-level 'permission' field."
            }

            if ($null -ne $config.permissions) {
                Fail "opencode.json contains V2 'permissions'; this bootstrap currently follows the live schema referenced by opencode.json."
            }

            if ($null -eq $config.permission.bash) {
                Fail "opencode.json must define permission.bash."
            }

            if ($null -eq $config.permission.edit) {
                Fail "opencode.json must protect privileged workflow files with permission.edit."
            }

            if ($null -eq $config.mcp.cua) {
                Fail "opencode.json must contain the disabled Cua MCP placeholder."
            }
            elseif ($config.mcp.cua.enabled -ne $false) {
                Fail "Tracked Cua MCP must remain disabled. Use scripts/opencode-cua.ps1 with a local runtime override."
            }

            Write-Host "[OK] OpenCode configuration parses and tracked safety invariants are present."
        }
        catch {
            Fail "opencode.json could not be parsed: $($_.Exception.Message)"
        }
    }

    if (Test-Path "global.json") {
        try {
            $globalJson = Get-Content "global.json" -Raw | ConvertFrom-Json
            if ($globalJson.sdk.version -notmatch '^10\.') {
                Fail "global.json must select .NET 10."
            }
            if ($globalJson.sdk.rollForward -ne "latestFeature") {
                Fail "global.json must keep rollForward=latestFeature for .NET 10 feature-band/patch flexibility."
            }
        }
        catch {
            Fail "global.json could not be parsed: $($_.Exception.Message)"
        }
    }

    $solutions = @(
        Get-ChildItem -Path . -File |
            Where-Object { $_.Extension -in @(".slnx", ".sln") }
    )

    if ($solutions.Count -eq 0) {
        Write-Host "[INFO] No .slnx or .sln file exists yet."
        Write-Host "[INFO] Documentation/bootstrap verification only."
    }
    elseif ($solutions.Count -gt 1) {
        Fail "Multiple solution files exist at the repository root. Keep one canonical Backseat solution or update verify.ps1 deliberately."
    }
    else {
        $solution = $solutions[0]
        Write-Host "[INFO] Solution: $($solution.Name)"

        & dotnet restore $solution.FullName
        if ($LASTEXITCODE -ne 0) { Fail "dotnet restore failed." }

        if (-not $failed) {
            & dotnet build $solution.FullName --no-restore
            if ($LASTEXITCODE -ne 0) { Fail "dotnet build failed." }
        }

        if (-not $failed) {
            & dotnet test $solution.FullName --no-build
            if ($LASTEXITCODE -ne 0) { Fail "dotnet test failed." }
        }
    }

    if ($failed) {
        Write-Host "[FAIL] Verification failed."
        exit 1
    }

    Write-Host "[OK] Verification complete."
    exit 0
}
finally {
    Pop-Location
}
