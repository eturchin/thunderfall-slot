# Cascade engine

Column-major index=column×Rows+row; row 0 is top. Thunderfall has 6×5 cells. Cell stores numeric SymbolId, optional multiplier, physical Identity and Collected flag; public snapshots are immutable.

Count all eight ordinary categories over 30 cells. Counts 8–9/10–11/12+ select one applicable band. Different categories may pay simultaneously. StormOrb and ThunderGate never pay as ordinary symbols. Evaluator returns a UInt64 removal mask and exact coefficient ticks.

Remove all winners simultaneously. For each column scan bottom-to-top and compact survivors to the bottom, preserving order. Fill top vacancies bottom-to-top in pinned draw order. New cells have new identities; survivors retain identity/value/collection. CascadeStep/events describe actual removals, drops, refills and resulting board.

Example top→bottom `[A,B,C,D,E]`, removing B/D leaves `[_,_,A,C,E]`. New draws X then Y (bottom vacancy first) produce `[Y,X,A,C,E]`.

## Termination

Specials never disappear, and each refill profile has permanent-special probability 0.05. Every continuing cascade refills at least eight cells. Special count is monotonic; eventually 23 of 30 cells are special, leaving fewer than eight ordinary cells, hence no win. Positive independent special arrivals prove almost-sure termination.

A conservative tail bound: continuation after k completed refills requires fewer than 23 special arrivals among at least 8k draws. Bound this with the corresponding lower Binomial(8k,0.05) tail, ignoring initial specials. It is intentionally loose.

The holdout's 55,041,155 total rounds reached maximum depth 13. MaxCascades=2048 is an operational failure boundary: exceeding it throws and rolls back, never clamps a payout. Host cancellation adds a time budget. Simulation aborts rather than silently dropping an expensive cycle. Tests force exhaustion and verify no wallet/round/idempotency writes.
