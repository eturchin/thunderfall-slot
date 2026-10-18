# ADR 003: Pinned deterministic cycle streams

Status: Accepted

## Context

Worker/thread/time seeding would make simulation results scheduling-dependent.

## Decision

Use public-domain xoshiro256**/SplitMix64 with explicit domain/chunk/cycle derivation; production uses crypto RNG.

## Consequences

Golden vectors, complete-cycle streams and fixed-order chunk merging are contractual; hashed seed separation is not proof of non-overlap.
