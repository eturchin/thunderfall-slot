$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
& (Join-Path $PSScriptRoot 'Start-Api.ps1')
Set-Location (Join-Path $projectRoot 'frontend')
npm.cmd ci
if($LASTEXITCODE -ne 0){throw 'npm ci failed'}
npm.cmd run dev
