param(
    [Parameter(Mandatory = $true)]
    [string[]]$Paths
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
        throw "Cannot stage autonomously from a detached HEAD or unborn branch."
    }

    if ($branch -eq "main" -or $branch -eq "master") {
        throw "Autonomous staging on '$branch' is not allowed. Create a topic/fix branch first."
    }

    if ($Paths.Count -eq 0) {
        throw "At least one explicit path is required."
    }

    foreach ($path in $Paths) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            throw "Empty paths are not allowed."
        }

        $normalized = $path.Replace('\', '/').Trim()

        if ($normalized -in @(".", "./", "..", "../")) {
            throw "Repository-wide or parent-directory staging is not allowed: '$path'."
        }

        if ($normalized.StartsWith("-")) {
            throw "Path may not look like a Git option: '$path'."
        }

        if ([System.IO.Path]::IsPathRooted($path)) {
            throw "Use repository-relative paths only: '$path'."
        }

        $segments = $normalized.Split('/', [System.StringSplitOptions]::RemoveEmptyEntries)
        if ($segments -contains "..") {
            throw "Parent traversal is not allowed: '$path'."
        }

        if ($path.IndexOfAny([char[]]"*?[]") -ge 0) {
            throw "Wildcards are not allowed. Stage explicit file paths: '$path'."
        }

        if (Test-Path -LiteralPath $path -PathType Container) {
            throw "Directories are not allowed. Stage explicit files: '$path'."
        }

        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            # Missing paths are allowed only when they are tracked deletions.
            & git ls-files --error-unmatch -- $path *> $null
            if ($LASTEXITCODE -ne 0) {
                throw "Path does not exist and is not a tracked deletion: '$path'."
            }
        }
    }

    Write-Host "Staging explicit files:"
    foreach ($path in $Paths) {
        Write-Host "  $path"
    }

    Invoke-Git add -- @Paths

    Write-Host ""
    Write-Host "Staged files:"
    Invoke-Git diff --staged --name-status
}
finally {
    Pop-Location
}
