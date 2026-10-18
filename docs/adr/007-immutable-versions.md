# ADR 007: Immutable versioned math definitions

Status: Accepted

## Context

Editing weights under an old identity would make historical behavior ambiguous.

## Decision

Validate/freeze definitions, canonical-hash sorted objects and preserve array order; pin version/hash per session.

## Consequences

Archive candidates/reports; keep old versions registered. Hash mismatch fails rather than silently migrating a session.
