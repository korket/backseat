param(
    [switch]$RequireCua
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $RepoRoot

try {
    $TestedOpenCodeVersion = "1.18.31"
    $RequiredDotNetMajor = 10

    Write-Host "Backseat environment check"
    Write-Host "=========================="

    function Check-Command {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Name
        )

        $cmd = Get-Command $Name -ErrorAction SilentlyContinue
        if ($null -eq $cmd) {
            Write-Host "[MISSING] $Name"
            return $false
        }

        Write-Host "[OK]      $Name -> $($cmd.Source)"
        return $true
    }

    $coreGood = $true

    $coreGood = (Check-Command "git") -and $coreGood
    $coreGood = (Check-Command "pwsh") -and $coreGood

    $openCodePresent = Check-Command "opencode"
    $coreGood = $openCodePresent -and $coreGood

    if ($openCodePresent) {
        try {
            $rawOpenCodeVersion = (& opencode --version 2>&1 | Out-String).Trim()
            Write-Host "          OpenCode $rawOpenCodeVersion"

            if ($rawOpenCodeVersion -match '(\d+\.\d+\.\d+)') {
                $installed = $Matches[1]
                if ($installed -ne $TestedOpenCodeVersion) {
                    Write-Host "[WARN]    Bootstrap was reviewed against OpenCode $TestedOpenCodeVersion; installed version is $installed."
                    Write-Host "          Re-check the live config schema if permission behavior differs."
                }
            }
            else {
                Write-Host "[WARN]    Could not parse OpenCode version."
            }
        }
        catch {
            Write-Host "[WARN]    opencode exists but version check failed: $($_.Exception.Message)"
        }
    }

    $dotnetPresent = Check-Command "dotnet"
    $coreGood = $dotnetPresent -and $coreGood

    if ($dotnetPresent) {
        try {
            $sdkLines = @(& dotnet --list-sdks 2>&1)
            $dotnet10 = @($sdkLines | Where-Object { $_ -match '^10\.\d+\.\d+' })

            if ($dotnet10.Count -eq 0) {
                Write-Host "[FAIL]    No installed .NET 10 SDK was found."
                $coreGood = $false
            }
            else {
                Write-Host "[OK]      Installed .NET 10 SDK:"
                foreach ($sdk in $dotnet10) {
                    Write-Host "          $sdk"
                }
            }

            $selectedVersion = (& dotnet --version 2>&1 | Out-String).Trim()
            if ($LASTEXITCODE -ne 0) {
                Write-Host "[FAIL]    global.json could not select a compatible .NET SDK."
                Write-Host "          $selectedVersion"
                $coreGood = $false
            }
            else {
                Write-Host "[OK]      Repository-selected SDK: $selectedVersion"
                if ($selectedVersion -notmatch '^10\.') {
                    Write-Host "[FAIL]    global.json did not select .NET 10."
                    $coreGood = $false
                }
            }
        }
        catch {
            Write-Host "[FAIL]    .NET SDK check failed: $($_.Exception.Message)"
            $coreGood = $false
        }
    }

    Write-Host ""
    Write-Host "Git readiness"
    Write-Host "-------------"

    $gitPresent = $null -ne (Get-Command "git" -ErrorAction SilentlyContinue)
    if ($gitPresent) {
        & git rev-parse --is-inside-work-tree *> $null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK]      Git repository detected"

            $branch = (& git branch --show-current 2>$null | Out-String).Trim()
            if ([string]::IsNullOrWhiteSpace($branch)) {
                Write-Host "[WARN]    No named current branch (detached HEAD or unborn branch)"
            }
            else {
                Write-Host "[OK]      Current branch: $branch"
            }
        }
        else {
            Write-Host "[MISSING] Git repository"
            $coreGood = $false
        }

        $gitName = (& git config --get user.name 2>$null | Out-String).Trim()
        if ([string]::IsNullOrWhiteSpace($gitName)) {
            Write-Host "[MISSING] git user.name"
            $coreGood = $false
        }
        else {
            Write-Host "[OK]      git user.name configured"
        }

        $gitEmail = (& git config --get user.email 2>$null | Out-String).Trim()
        if ([string]::IsNullOrWhiteSpace($gitEmail)) {
            Write-Host "[MISSING] git user.email"
            $coreGood = $false
        }
        else {
            Write-Host "[OK]      git user.email configured"
        }
    }

    Write-Host ""
    Write-Host "Cua Driver readiness"
    Write-Host "--------------------"

    $cuaPresent = Check-Command "cua-driver"
    $cuaGood = $true

    if ($cuaPresent) {
        Write-Host ""
        Write-Host "Version:"
        & cua-driver --version
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[WARN]    cua-driver version check failed."
            $cuaGood = $false
        }

        Write-Host ""
        Write-Host "Doctor:"
        & cua-driver doctor
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[WARN]    cua-driver doctor reported a command failure."
            $cuaGood = $false
        }
        else {
            Write-Host "[INFO]    Read doctor warnings too; a zero exit code alone does not prove desktop readiness."
        }

        Write-Host ""
        Write-Host "OpenCode MCP compatibility:"
        & cua-driver mcp-config --client opencode
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[WARN]    Cua Driver could not generate current OpenCode MCP guidance."
            $cuaGood = $false
        }
        else {
            Write-Host "[OK]      Cua Driver recognizes OpenCode as an MCP client."
        }

        Write-Host ""
        Write-Host "Persistent daemon (optional for bare MCP on Windows/Linux):"
        & cua-driver status
        $daemonRunning = $LASTEXITCODE -eq 0

        if ($daemonRunning) {
            Write-Host ""
            Write-Host "Visible applications through daemon:"
            $appsOutput = (& cua-driver call list_apps 2>&1 | Out-String).Trim()
            if ($LASTEXITCODE -ne 0) {
                Write-Host $appsOutput
                Write-Host "[WARN]    list_apps failed against the running daemon."
                $cuaGood = $false
            }
            elseif ([string]::IsNullOrWhiteSpace($appsOutput)) {
                Write-Host "[WARN]    list_apps returned no visible output."
                $cuaGood = $false
            }
            else {
                Write-Host $appsOutput
                Write-Host "[INFO]    Confirm the output includes a GUI application you recognize."
            }
        }
        else {
            Write-Host "[INFO]    No persistent Cua daemon detected."
            Write-Host "[INFO]    This is not fatal for the Windows/Linux bare 'cua-driver mcp' path,"
            Write-Host "          which can own its own runtime. Desktop access must still be verified in the MCP experiment."
        }
    }
    else {
        $cuaGood = $false
        if ($RequireCua) {
            Write-Host "[MISSING] Cua Driver is required for the current task."
        }
        else {
            Write-Host "[INFO]    Cua Driver is optional until a compatibility task requires it."
        }
    }

    Write-Host ""
    if (-not $coreGood) {
        Write-Host "Environment is not ready for autonomous Backseat development."
        exit 1
    }

    if ($RequireCua -and -not $cuaGood) {
        Write-Host "Core development tools are ready, but the Cua installation/client checks failed."
        exit 1
    }

    Write-Host "Core development environment is ready."
    if ($RequireCua) {
        Write-Host "Cua installation/client checks passed. Verify actual desktop access through the MCP compatibility experiment."
    }
    elseif (-not $cuaGood) {
        Write-Host "Cua Driver readiness is incomplete; use -RequireCua when it becomes mandatory."
    }
    exit 0
}
finally {
    Pop-Location
}
