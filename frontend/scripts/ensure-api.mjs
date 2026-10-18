import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

try {
    const response = await fetch('http://127.0.0.1:5090/health/ready', {
        signal: AbortSignal.timeout(2000),
    });
    if (response.ok && (await response.json()).status === 'ready') process.exit(0);
} catch {
    // Start the local backend below when it is unavailable.
}

if (process.platform !== 'win32') {
    console.error('API unavailable on port 5090. Configure ConnectionStrings__Game, run dotnet run --project ../src/Platform/Thunderfall.Api -- --migrate, then dotnet run --project ../src/Platform/Thunderfall.Api --urls http://127.0.0.1:5090.');
    process.exit(1);
}

const result = spawnSync('powershell.exe', [
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    fileURLToPath(new URL('../../scripts/Start-Api.ps1', import.meta.url)),
], { stdio: 'inherit', windowsHide: true });
if (result.error) console.error(result.error.message);
process.exit(result.status ?? 1);
