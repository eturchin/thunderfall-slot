# API contract

OpenAPI `/openapi/v1.json`; camelCase DTOs. Every monetary field, including request stake, is integer minor units (100=1 credit). Stake 2000 means 20 credits. No mixed request/response units.

| Method | Route | Result |
|---|---|---|
| GET | `/api/v1/games` | Current definitions/versions, layouts, supported stakes, symbol IDs |
| GET | `/api/v1/games/{id}?version=1.0.0` | Current or registered historical public metadata |
| GET | `/api/v1/games/{id}/rules?version=1.0.0` | Exact ticks/scales, feature rules and cap |
| POST | `/api/v1/sessions` | `{gameId:"thunderfall"}` => 201 SessionView and cookie |
| GET | `/api/v1/sessions/current` | Authenticated wallet, pinned version/hash, bonus state |
| POST | `/api/v1/rounds` | RoundCommand => committed RoundResponse |
| GET | `/api/v1/rounds/{roundId}` | Full stored result for read-only replay, session restricted |
| GET | `/api/v1/rounds/history?offset=0&limit=20` | Metadata only; nonnegative offset, limit 1–50 |
| GET | `/api/v1/features/current` | Persisted mathematical state |
| POST | `/api/v1/sessions/reset` | `{}` => explicit ledger reset, ending bonus |
| GET | `/health/live` | Process liveness |
| GET | `/health/ready` | Database reachability |

POST requires JSON, `X-Thunderfall-Client: web` and same-origin Origin/Fetch Metadata when present. Credentials use HttpOnly/Strict `tf_session`, path `/api/v1`, Secure on HTTPS. ID alone is insufficient. CORS is not opened.

Base command: `{"stake":2000,"idempotencyKey":"ba049e96-caf2-40e1-8863-e33113e0bd30"}`. Omit stake for free mode; explicit bonus stake must equal OriginalStake. Server chooses mode and crypto RNG. Unknown JSON members are rejected.

Response includes top-level RoundId/GameId/MathVersion/Mode/Stake, InitialBoard, CascadeSteps, WinningGroups, MultiplierEvents, RawWin, FinalWin, BalanceAfter, FeatureState and PresentationHints. `outcome` also retains hash, exact pay ticks, starting/final states, terminal board, mathematical events, nominal award and cap explanation. Entire response is persisted; a retry/replay is not recalculated under a new definition.

Idempotency is session scoped. Identical key+normalized optional stake returns the committed original response. Different payload conflicts with 409. After ambiguous failures retry the saved command/key, not a new key. Browser refreshes current session before reconciliation so an old committed response cannot move a newer wallet backward.

ProblemDetails title/code: SessionNotFound/RoundNotFound/InvalidGame (404); InvalidStake/InvalidIdempotencyKey (400); InsufficientBalance/IdempotencyConflict/MathVersionConflict (409); mathematical resource/arithmetic failures (422); InvalidOrigin (403); rate limit (429). Unhandled errors are sanitized. Request maximum 16 KiB; outcome maximum 8 MiB is checked before commit. History is lightweight/bounded.

Session row locking serializes distinct concurrent requests; a public SpinAlreadyInProgress response is unnecessary. Readiness checks connectivity, so schema migration must be a separate successful deployment step. Rate limit is process-local, 240 requests/minute per observed IP; a proxy may group clients.

## Complete archive and Bonus Buy additions

- `GET /api/v1/rounds/history/page?page=1&pageSize=20`: authenticated metadata page `{items,total,page,pageSize}`; positive page, size 1-50, checked integer offset. All records remain stored. Invalid pagination returns 400.
- `POST /api/v1/sessions/game` with `{gameId}`: keeps the same session/token, balance and archive; rejects active bonuses.
- `POST /api/v1/rounds` additionally accepts `buyBonus:true` with a supported stake. The same operation key includes purchase mode in its fingerprint. A purchased first-free-spin response has `mode:BonusBuy`, the genuine free-spin outcome and `bonusPurchase:{policyVersion,cost,awardedSpins}`. The rules endpoint publishes available Bonus Buy options. See BONUS_BUY.md.
- Legacy history array endpoint remains compatible; the former 100000-offset clamp is removed. Page size remains bounded to keep response sizes reasonable.

History queries project metadata in SQL and do not load `OutcomeJson`. Within one paginated response, items and total share a database snapshot, including empty pages beyond the end. Separate page requests can observe newly committed rounds.

The browser persists a pending command before sending each round attempt. If persistence fails, it sends no round and keeps retry available. Once the round endpoint succeeds, an error refreshing the session retains that command/key for recovery, including after reload. Failure to remove a completed key from browser storage does not invalidate the confirmed response; a retained key can only resolve the original operation.
