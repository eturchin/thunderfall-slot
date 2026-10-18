$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
. (Join-Path $PSScriptRoot 'Start-LocalDatabase.ps1')
$env:TF_TEST_POSTGRES=$env:ConnectionStrings__Game
dotnet restore --locked-mode
if($LASTEXITCODE -ne 0){throw 'Restore failed'}
dotnet build -c Release --no-restore
if($LASTEXITCODE -ne 0){throw 'Build failed'}
dotnet test -c Release --no-build --logger trx
if($LASTEXITCODE -ne 0){throw 'Backend verification failed'}
Push-Location frontend
try{npm.cmd ci;if($LASTEXITCODE -ne 0){throw 'npm ci failed'};npm.cmd run lint;if($LASTEXITCODE -ne 0){throw 'Lint failed'};npm.cmd run build;if($LASTEXITCODE -ne 0){throw 'Frontend build failed'};npm.cmd test;if($LASTEXITCODE -ne 0){throw 'Frontend tests failed'}}finally{Pop-Location}
