# Adding a game module

1. Create a unique lowercase GameId and semantic MathVersion in an immutable versioned math folder. Start from Classic for reel/payline or frozen Thunderfall for weighted count-anywhere. Keep SVG/CSS/theme separate.
2. Declare layout, symbols, ascending count bands in millionth-stake ticks, integer profiles/orb values, minor-unit stakes, cycle cap and operational budgets. Zero weights are allowed, zero total is not. Refill needs permanent-special mass; bonus offspring must pass subcriticality validation.
3. Compile/validate before registering. Unknown symbols, invalid probabilities/geometry, unsupported orb values and overflow-prone bounds fail startup rather than deferring to a spin.
4. Compose RoundEngine strategies. Current geometries are CountAnywhere and Paylines. A new geometry needs evaluator/trace/reference tests and explicit registration; do not expose dummy Ways/Cluster implementations.
5. Register IGameMathModel instances with GameRegistry in the API composition root. Keys are (GameId,MathVersion), not GameId branches in wallet code. Keep old versions registered when adding a release, so pinned sessions still execute. Content edits behind the same version fail the stored hash check.
6. Current game metadata selects the latest registered semantic release. Public game/rules endpoints accept an explicit version; the frontend requests the session's pinned definition. Old results contain complete snapshots/events and replay without settlement.
7. Add assets and presentation hints where needed. The renderer consumes IDs/positions/removals/refills, not mathematical rules. Classic already reuses runtime, wallet, sessions, API, simulator and board playback without copying a service.
8. Run unit/property/replay/architecture, exact/reference checks, worker/cross-process golden tests, calibration and reserved holdout. Freeze/archive definitions and reports, then run PostgreSQL/browser verification.

A dynamic DLL plugin loader or arbitrary graph VM is unnecessary here. Explicit registrations keep the mathematics inspectable. New definitions must never mutate during running rounds or silently change the interpretation of historical spins.
