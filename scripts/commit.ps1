param(
    [Parameter(Mandatory = $true)]
    [string]$Subject,

    [string]$Body
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $RepoRoot

try {
    function Invoke-Git {
        param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)

        & git @Args
        if ($LASTEXITCODE -ne 0) {
            throw "git $($Args -join ' ') failed with exit code $LASTEXITCODE."
        }
    }

    $branch = (& git branch --show-current 2>$null | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($branch)) {
        throw "Cannot commit autonomously from a detached HEAD or unborn branch."
    }

    if ($branch -eq "main" -or $branch -eq "master") {
        throw "Autonomous commits on '$branch' are not allowed."
    }

    $gitDir = (& git rev-parse --git-dir 2>$null | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($gitDir)) {
        throw "Not inside a Git repository."
    }

    if (-not [System.IO.Path]::IsPathRooted($gitDir)) {
        $gitDir = Join-Path $RepoRoot $gitDir
    }

    $blockedStates = @(
        (Join-Path $gitDir "MERGE_HEAD"),
        (Join-Path $gitDir "CHERRY_PICK_HEAD"),
        (Join-Path $gitDir "REVERT_HEAD"),
        (Join-Path $gitDir "rebase-merge"),
        (Join-Path $gitDir "rebase-apply")
    )

    foreach ($state in $blockedStates) {
        if (Test-Path $state) {
            throw "Repository is in a merge/rebase/cherry-pick/revert state. Autonomous commit is blocked."
        }
    }

    $subjectTrimmed = $Subject.Trim()
    if ($subjectTrimmed.Length -gt 72) {
        throw "Commit subject must be 72 characters or fewer."
    }

    if ($subjectTrimmed -notmatch '^[a-z0-9][a-z0-9._-]*: .+\S$') {
        throw "Commit subject must use '<area>: <imperative summary>'."
    }

    if ($subjectTrimmed -match '^(wip|fix|oops|temp|tmp):' -or
        $subjectTrimmed -match '\b(wip|oops|temporary|debug)\b') {
        throw "Throwaway/debug commit subjects are not allowed."
    }

    # No diff filter: type-only changes and every other staged change count.
    $staged = @(& git diff --staged --name-only 2>$null)
    $staged = @($staged | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    if ($staged.Count -eq 0) {
        throw "Nothing is staged."
    }

    $forbiddenPatterns = @(
        '(^|/)(bin|obj|TestResults|artifacts)(/|$)',
        '^runs/(?!\.gitkeep$)',
        '^\.backseat-local(/|$)',
        '(^|/)\.env($|\.)'
    )

    foreach ($path in $staged) {
        $normalized = $path.Replace('\', '/')
        foreach ($pattern in $forbiddenPatterns) {
            if ($normalized -match $pattern) {
                throw "Generated, runtime, or local-secret artifact cannot be auto-committed: '$path'."
            }
        }
    }

    & git diff --staged --check
    if ($LASTEXITCODE -ne 0) {
        throw "git diff --staged --check failed. Fix whitespace/errors before committing."
    }

    Write-Host "Commit preview"
    Write-Host "=============="
    Write-Host "Branch:  $branch"
    Write-Host "Subject: $subjectTrimmed"
    Write-Host ""
    Write-Host "Files:"
    foreach ($path in $staged) {
        Write-Host "  $path"
    }
    Write-Host ""
    Invoke-Git diff --staged --stat

    $args = @("commit", "-m", $subjectTrimmed)
    if (-not [string]::IsNullOrWhiteSpace($Body)) {
        $args += @("-m", $Body.Trim())
    }

    & git @args
    if ($LASTEXITCODE -ne 0) {
        throw "git commit failed with exit code $LASTEXITCODE."
    }

    Write-Host ""
    Write-Host "[OK] Commit created."
    Invoke-Git show --stat --oneline --summary HEAD
}
finally {
    Pop-Location
}
