# ADR 002: Weighted cells and a separate reel strategy

Status: Accepted

## Context

The games have different generation semantics. Treating tumbles as independently redrawn full boards is incorrect.

## Decision

Thunderfall uses exact integer weighted initial/refill samplers; Classic uses cyclic reel strips.

## Consequences

Shared numeric cells/outcomes, separate generation/evaluation rules. Surviving cells retain identity.
