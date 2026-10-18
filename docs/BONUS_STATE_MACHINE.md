# Bonus state machine

4+ ThunderGate symbols on the initial paid board award 15 spins. Later tumble scatters do not trigger. Each free spin consumes one spin; 3+ gates on that spin's initial board award five more. The bonus starts at persistent ×1; retriggers retain original stake and multiplier.

State: BonusSessionId (trigger RoundId), GameId, MathVersion, OriginalStake, RemainingSpins, PlayedSpins, PersistentMultiplier, AccumulatedWin, RetriggerCount, StateVersion, CycleAward. AccumulatedWin includes bonus awards only; CycleAward also includes the triggering base award. StateVersion=1 is the schema contract; persisted feature/session Version increments on committed operations.

Example: Remaining=2 and multiplier=7. A spin collects 5 and has three gates. New multiplier=12; Remaining=2−1+5=6. Settle using twelve and add the actual award to bonus/cycle totals. Every spin is committed separately; disconnect never fast-forwards the bonus.

## Ending and resources

Bonus initial scatter p=15/1000. Let q=P(Binomial(30,p)≥3). Each consumed spin produces five spins with probability q. Expected offspring 5q<1 proves almost-sure termination of the uncapped subcritical process. Expected initial length is 15/(1−5q). Validation enforces a conservative union-bound subcriticality check. The cap is an additional absorbing state, not the argument for natural ending.

No mathematical retrigger-count limit exists. MaxBonusRounds=100000 is an operational fail-without-commit boundary, not a forced paid settlement or silent forfeiture. An exhausted production operation would require a recovery workflow; this demo returns the failure and supports an explicit ledger-recorded reset.

## Paid-cycle maximum

Cap=OriginalStake×5000. RemainingAllowance=Cap−prior CycleAward. FinalWin=min(rounded nominal round award, RemainingAllowance). At the cap, record MaxWinReached (nominal, paid, cumulative and abandoned-spin data), end the bonus and emit FeatureCompleted. No individual cascade is clamped.

At stake 2,000, cap=10,000,000 minor units. If prior awards=9,999,983, only 17 more units can be credited (0.17 credit), regardless of a larger nominal round. The event records why and how many spins were abandoned.

Integration tests preserve the HttpOnly cookie across an actual API child-process kill/restart and verify that the persisted bonus resumes without a stake debit. Test-only setup writes genuine engine-generated feature state directly to an isolated test database; no seed/debug HTTP endpoint is introduced.
