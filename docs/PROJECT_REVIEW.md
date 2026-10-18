# Project review — 2026-10-18

Reviewed session persistence, history queries, HTTP/session boundaries, round orchestration, replay validation, simulation aggregation and client recovery paths. This pass prioritized reproducible accounting/recovery failures and unnecessary history data loading. Mathematical rules, settlement and archived definitions were not changed.

## Findings addressed

| Priority | Weakness | Change and evidence |
|---|---|---|
| High | After a successful round, a 400/403/404/409/422 response from session refresh deleted its idempotency key. The next click could start another paid round. | Track whether the round endpoint accepted the command and retain its key when subsequent reconciliation fails. Five regression cases failed before and pass after. Chromium verifies recovery after reload returns the same round ID, one history record and the committed balance. |
| High | Saving the pending key happened outside `try/finally`. A storage exception left controls locked and caused an unhandled promise rejection. | Persist inside the guarded workflow before every attempt. Storage failure sends no round, keeps the same key in memory and releases controls. Unit and browser tests cover repeated failure and unit tests cover recovery. |
| Medium | Failure deleting a completed key converted a confirmed round into a misleading connection error. | Encapsulate pending-command persistence and make completed-key removal best effort. A retained key resolves the existing operation; the next command overwrites it. Regression verifies the confirmed balance and usable controls. |
| Medium | History loaded complete `RoundRecord` entities, including potentially large replay JSON that the response discarded. | Project `RoundHistoryItem` fields directly in SQL for both history endpoints. A command interceptor confirms queries exclude `OutcomeJson`; this test failed on the original implementation. |
| Medium | Page items and total came from independent reads. A round committed between them produced an inconsistent response. | Read both in one `REPEATABLE READ` transaction. A deterministic PostgreSQL test commits another round between reads: the response remains at one item/one total, the next request sees two, and an empty page retains the correct total. |

## Verification

- Release solution build: 0 warnings, 0 errors.
- Backend: 67 passed, 0 skipped (42 math/game/simulation/property, 19 PostgreSQL/API, 6 architecture). Includes traced/untraced equality, replay, worker-count determinism, ledger reconciliation, rollback and process restart.
- Frontend: TypeScript checks, ESLint, production build and 18 component tests passed.
- Chromium: all 7 scenarios passed in 23.9 seconds, including two added recovery scenarios. Used the workspace Vite development server and a fresh Release API against a temporary PostgreSQL database; the temporary API/database were removed afterwards. Existing report screenshots were restored after the suite.
- CSharpier and `dotnet format ... style --verify-no-changes` passed for the four changed C# files. Prettier 3.6.2 check passed for the four changed TypeScript/TSX files; it was invoked as a temporary formatting tool without changing project dependencies.
- No load benchmark, Docker startup, Firefox/WebKit run or production-bundle browser run was performed. Removing replay payload selection is verified from SQL, but no latency/throughput improvement is claimed.

## Remaining architectural work

- `App.tsx` still combines session initialization, round/replay flows, settings and modal markup. Extract these by responsibility in small steps while keeping recovery and browser tests as behavioral constraints.
- Offset pagination remains increasingly expensive at deep offsets and does not freeze history across page requests. Introduce a cursor and an archive snapshot boundary if measured usage requires stable traversal of large, actively growing histories.
- Formatting checks currently cover the edited files; CI does not yet enforce a repository-wide formatting baseline. Establish that baseline separately to keep behavioral changes reviewable.
- Deployment and device/browser verification limits remain tracked in `PROGRESS.md`.
