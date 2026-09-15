param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

if (-not $SkipBuild) {
    dotnet build "$root\TimeTracker.slnx" --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}

$process = Start-Process dotnet -ArgumentList 'run --project App.Web --no-build --urls http://127.0.0.1:5188' -WorkingDirectory $root -PassThru -WindowStyle Hidden
try {
    $deadline = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 500
        try {
            $response = Invoke-WebRequest -Uri 'http://127.0.0.1:5188/health' -UseBasicParsing
        } catch {
            $response = $null
        }
    } while ($null -eq $response -and (Get-Date) -lt $deadline)

    if ($null -eq $response -or $response.StatusCode -ne 200 -or $response.Content -notmatch '"ok"') {
        throw 'Health check failed.'
    }

    Write-Host 'Smoke test passed: application built and health endpoint responded.'
}
finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
}
