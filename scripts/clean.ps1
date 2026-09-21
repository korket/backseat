$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $RepoRoot

try {
    Write-Host "Cleaning generated build output"

    Get-ChildItem -Path . -Directory -Recurse -Force |
        Where-Object { $_.Name -in @("bin", "obj", "TestResults") } |
        ForEach-Object {
            Write-Host "Removing $($_.FullName)"
            Remove-Item $_.FullName -Recurse -Force
        }

    Write-Host "Done."
}
finally {
    Pop-Location
}
