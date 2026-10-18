param([string]$PostgresBin=$env:POSTGRES_BIN)
$ErrorActionPreference='Stop'
if ([string]::IsNullOrWhiteSpace($PostgresBin)) {
    $pgCtl = Get-Command pg_ctl.exe -ErrorAction SilentlyContinue
    if ($pgCtl) {
        $PostgresBin = Split-Path $pgCtl.Source -Parent
    } elseif ($env:ProgramFiles) {
        $PostgresBin = Join-Path $env:ProgramFiles 'PostgreSQL/16/bin'
    }
}
if ([string]::IsNullOrWhiteSpace($PostgresBin) -or !(Test-Path -LiteralPath (Join-Path $PostgresBin 'pg_ctl.exe'))) {
    throw 'PostgreSQL tools not found. Add the PostgreSQL bin directory to PATH, set POSTGRES_BIN, or pass -PostgresBin.'
}
$projectRoot=Split-Path $PSScriptRoot -Parent
$localDirectory=Join-Path $projectRoot '.local'
New-Item -ItemType Directory -Force -Path $localDirectory | Out-Null
$passwordFile=Join-Path $localDirectory 'db-password.txt'
if(!(Test-Path -LiteralPath $passwordFile)){$bytes=New-Object byte[] 32;[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes);[IO.File]::WriteAllText($passwordFile,[Convert]::ToBase64String($bytes))}
$dbPassword=[IO.File]::ReadAllText($passwordFile)
$dataDirectory=Join-Path $localDirectory 'pgdata'
if(!(Test-Path -LiteralPath (Join-Path $dataDirectory 'PG_VERSION'))){
 & (Join-Path $PostgresBin 'initdb.exe') -D $dataDirectory -U thunderfall -A scram-sha-256 --pwfile=$passwordFile --encoding=UTF8 --locale=C
 if($LASTEXITCODE -ne 0){throw 'initdb failed'}
 Add-Content -LiteralPath (Join-Path $dataDirectory 'postgresql.conf') -Value "`nlisten_addresses = '127.0.0.1'`nport = 55433"
}
& (Join-Path $PostgresBin 'pg_ctl.exe') status -D $dataDirectory | Out-Null
if($LASTEXITCODE -ne 0){& (Join-Path $PostgresBin 'pg_ctl.exe') start -D $dataDirectory -l (Join-Path $localDirectory 'postgres.log') -w;if($LASTEXITCODE -ne 0){throw 'PostgreSQL startup failed'}}
$env:PGPASSWORD=$dbPassword
$exists=& (Join-Path $PostgresBin 'psql.exe') -h 127.0.0.1 -p 55433 -U thunderfall -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='thunderfall'"
if($exists -ne '1'){& (Join-Path $PostgresBin 'createdb.exe') -h 127.0.0.1 -p 55433 -U thunderfall thunderfall;if($LASTEXITCODE -ne 0){throw 'createdb failed'}}
Remove-Item Env:PGPASSWORD
$env:ConnectionStrings__Game="Host=127.0.0.1;Port=55433;Database=thunderfall;Username=thunderfall;Password=$dbPassword"
Write-Host 'Dedicated Thunderfall PostgreSQL ready on 127.0.0.1:55433; connection configured in this PowerShell session.'
