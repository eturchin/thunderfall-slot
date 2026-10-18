# ADR 005: Single round settlement and paid-cycle cap

Status: Accepted

## Context

Per-tumble rounding or hidden per-tumble caps change the mathematics.

## Decision

Accumulate exact raw coefficients, apply multiplier, round once, then enforce remaining 5000x cycle allowance.

## Consequences

Nominal versus paid award and abandoned feature state are explicit events. Overflow/work failures roll back.
