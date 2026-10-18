# Architecture

A modular monolith with a pure deterministic mathematical boundary. The HTTP application is one deployable API; PostgreSQL provides durable state and serialization. No Redis, queues, graph execution or service mesh is required.

Each session-store operation creates and disposes its own `GameDbContext` through `IDbContextFactory<GameDbContext>`. A write operation owns both the context and the database transaction: it locks the session row with `FOR UPDATE`, loads the wallet and feature state, runs the application callback, and commits all changes together. `PostgresSessionTransaction` exposes the application transaction contract without exposing EF Core. A failed callback or save disposes the transaction and its tracked entities, so reusing the store starts from committed database state. Sequential operations are separate units of work; changes requiring atomicity belong inside one `ExecuteAsync` callback.

History is a dedicated read projection: EF selects only `RoundHistoryItem` fields, leaving replay JSON in the database until an individual replay is requested. A paginated response reads items and total inside one PostgreSQL `REPEATABLE READ` transaction, so a concurrent round cannot make those two values disagree. Different page requests use independent snapshots; offset pagination does not provide a frozen archive across multiple requests.

| Layer | Responsibility | Dependencies |
|---|---|---|
| Primitives | Numeric symbols, cell identity, immutable boards, exact settlement | BCL |
| Randomness | Crypto RNG, pinned xoshiro256**, integer weighted sampler | Building blocks |
| Engine.Abstractions | Immutable definitions, requests, states, outcomes and typed events | Building blocks |
| Engine.Evaluators | Count-anywhere, paylines and independent reference evaluation | Abstractions |
| Engine.Features | Bonus/retrigger, multiplier and cycle-cap settlement | Abstractions |
| Engine.Core | Generation, stable gravity, orchestration, replay validation | Evaluators + Features |
| Games.Storm / Games.Classic | Configuration loading and strategy composition | Core |
| Engine.Simulation | Deterministic cycles, bounded chunks, moments/exact checks | Abstractions |
| Application | Session commands, wallet invariants, idempotency, presentation adapter | Abstractions |
| Infrastructure | EF Core/Npgsql, six tables and atomic transactions | Application |
| Api | HTTP/cookies, CSRF, typed DTOs, rate limiting, OpenAPI | Platform + modules |
| Tools | Simulator CLI and HTML/JSON/CSV reporting | Engine/modules |

Mathematics has no HTTP, EF, database, wallet, system clock or static mutable RNG dependency. RoundId is an explicit input and becomes the bonus identity. The same definition, request, initial state and RNG stream produce identical outcomes. Architecture tests reject platform references in mathematical assemblies.

Definitions are immutable records/collections. Compilation validates them, caches a canonical SHA-256 hash, precomputes payout tables and creates private integer CDF samplers. Round scratch boards/counters/histograms are stack-allocated. Physical cell identity and Collected flag travel with an orb under gravity. Trace-off uses the same loop without snapshots/event lists. Differential properties compare trace-on/off settlements and states.

CountAnywhere and Paylines are the supported geometries, selected by definition kind rather than GameId. New games register models with GameRegistry. Classic proves reuse of sessions, wallet, versioning, API, simulator and rendering. A new geometry requires a validated strategy and trace/reference tests, not a copied game service.

React owns controls/dialogs; PixiJS owns the board; GSAP animates positions supplied by the engine. Presentation hints derive from mathematical events in Application and never enter settlement. Offline trace fixtures come from the C# CLI and are test-only. See ADRs for tradeoffs. This is a portfolio demo, not a certified gambling system.
