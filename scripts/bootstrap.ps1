param(
    [switch]$InitializeGit,
    [switch]$CommitBootstrap
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $RepoRoot

try {
    Write-Host "Backseat bootstrap"
    Write-Host "=================="

    function Command-Exists {
        param([Parameter(Mandatory = $true)][string]$Name)
        return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
    }

    if (-not (Command-Exists "git")) {
        throw "Git is required."
    }

    $insideRepo = $false
    & git rev-parse --is-inside-work-tree *> $null
    if ($LASTEXITCODE -eq 0) {
        $insideRepo = $true
    }

    if (-not $insideRepo) {
        if (-not $InitializeGit) {
            Write-Host ""
            Write-Host "No Git repository found."
            Write-Host "Run with -InitializeGit to create one with main as the initial branch."
            exit 1
        }

        Write-Host "Initializing Git repository on main..."
        & git init -b main
        if ($LASTEXITCODE -ne 0) {
            throw "git init failed."
        }
        $insideRepo = $true
    }
    else {
        Write-Host "[OK] Existing Git repository detected."
    }

    if (-not $CommitBootstrap) {
        Write-Host ""
        Write-Host "Bootstrap files are ready."
        Write-Host "No commit was created."
        Write-Host ""
        Write-Host "Next:"
        Write-Host "  pwsh ./scripts/doctor.ps1"
        exit 0
    }

    & git rev-parse --verify HEAD *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "[STOP] Repository history already exists."
        Write-Host "The automatic bootstrap commit is only for a brand-new repository."
        Write-Host "Review and commit these files manually instead."
        exit 1
    }

    $name = (& git config --get user.name 2>$null | Out-String).Trim()
    $email = (& git config --get user.email 2>$null | Out-String).Trim()

    if ([string]::IsNullOrWhiteSpace($name) -or [string]::IsNullOrWhiteSpace($email)) {
        Write-Host ""
        Write-Host "[STOP] Git author identity is not configured."
        Write-Host "Configure user.name and user.email, then retry."
        exit 1
    }

    $manifestPath = "BOOTSTRAP-FILES.txt"
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Missing $manifestPath."
    }

    $bootstrapFiles = @(
        Get-Content -LiteralPath $manifestPath -Encoding UTF8 |
            ForEach-Object { ([string]$_).TrimStart([char]0xFEFF).Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith("#") }
    )

    if ($bootstrapFiles.Count -eq 0) {
        throw "$manifestPath contains no files."
    }

    $missing = @()
    foreach ($path in $bootstrapFiles) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            $missing += $path
        }
    }

    if ($missing.Count -gt 0) {
        Write-Host "[STOP] Bootstrap package is incomplete. Missing:"
        foreach ($path in $missing) {
            Write-Host "  $path"
        }
        exit 1
    }

    Write-Host "Staging exact bootstrap package files..."
    foreach ($path in $bootstrapFiles) {
        & git add -- $path
        if ($LASTEXITCODE -ne 0) {
            throw "git add failed for '$path'."
        }
    }

    Write-Host ""
    Write-Host "Staged diff:"
    & git diff --staged --stat
    if ($LASTEXITCODE -ne 0) {
        throw "git diff failed."
    }

    Write-Host ""
    Write-Host "Creating initial bootstrap commit..."
    & git commit -m "build: bootstrap Backseat repository"
    if ($LASTEXITCODE -ne 0) {
        throw "git commit failed."
    }

    Write-Host ""
    Write-Host "[OK] Initial Backseat bootstrap commit created."
    Write-Host "Next: pwsh ./scripts/doctor.ps1"
}
finally {
    Pop-Location
}
