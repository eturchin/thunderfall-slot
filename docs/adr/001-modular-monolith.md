# ADR 001: Modular monolith

Status: Accepted

## Context

A service mesh adds failure surfaces without scaling evidence. Explicit layer tests protect the core.

## Decision

One deployable API with pure engine modules and a single transactional PostgreSQL store.

## Consequences

Per-session row locks work across API instances; global operations and distributed rate limiting remain future work.
