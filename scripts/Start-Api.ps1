$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$readyUrl = 'http://127.0.0.1:5090/health/ready'

function Test-ApiReady {
    try { return (Invoke-RestMethod $readyUrl -TimeoutSec 2).status -eq 'ready' }
    catch { return $false }
}

if (Test-ApiReady) { return }
if (!(Test-Path Env:ConnectionStrings__Game)) {
    . (Join-Path $PSScriptRoot 'Start-LocalDatabase.ps1')
}
$localDirectory = Join-Path $projectRoot '.local'
New-Item -ItemType Directory -Force -Path $localDirectory | Out-Null
Push-Location $projectRoot
try {
    dotnet run --project src/Platform/Thunderfall.Api -- --migrate
    if ($LASTEXITCODE -ne 0) { throw 'API migration/build failed; frontend startup cancelled.' }
    $outputLog = Join-Path $localDirectory 'api.log'
    $errorLog = Join-Path $localDirectory 'api-error.log'
    $process = Start-Process -FilePath 'dotnet' -ArgumentList @('run', '--project', 'src/Platform/Thunderfall.Api', '--no-build', '--urls', 'http://127.0.0.1:5090') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $outputLog -RedirectStandardError $errorLog
    $process.Id | Set-Content -LiteralPath (Join-Path $localDirectory 'api.pid')
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) { throw "API exited before becoming ready. See $outputLog and $errorLog" }
        if (Test-ApiReady) {
            Write-Host 'Thunderfall API ready on http://127.0.0.1:5090'
            return
        }
        Start-Sleep -Milliseconds 300
    }
    Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
    throw "API readiness timed out. See $outputLog and $errorLog"
}
finally { Pop-Location }
