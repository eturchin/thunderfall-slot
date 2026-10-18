# ADR 006: Typed mathematical events and replay

Status: Accepted

## Context

Animation-driven inference cannot be an audit trail.

## Decision

Versioned discriminated events carry actual cells/positions/amounts, separate from presentation hints.

## Consequences

ReplayValidator rebuilds boards and cross-checks reference wins/settlement. JSONB metadata ordering is supported explicitly.
