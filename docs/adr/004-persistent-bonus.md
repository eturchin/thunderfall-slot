# ADR 004: Persist each bonus round independently

Status: Accepted

## Context

Pre-generating a bonus hides interruption boundaries and complicates replay/idempotency.

## Decision

Store the complete immutable bonus state alongside wallet/outcome in one transaction.

## Consequences

Reload/process restart resumes the next free spin; original stake/version/multiplier remain pinned.
