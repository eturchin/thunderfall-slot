# Simulation verification

The simulator calls IGameMathModel.ExecuteSummary, the same loop/settlement as traced API execution. It never serializes individual rounds or stores per-round lists on the ordinary hot path. Scratch cells/counts/histograms use stack spans; immutable feature records are allocated only when needed. CLI serialization happens after aggregation or in explicit offline trace mode.

One observation is a **complete paid cycle**, including all free spins/retriggers. Wager counts paid cycles only. TotalBaseWin+TotalBonusWin=TotalAward. Contributions sum to observed RTP. Hit means any positive cycle award. Retrigger frequency is retriggers/bonus spins. Multiplier frequency counts rounds with eligible collection, not idle appearances; value distribution counts collected physical orbs. Maximum observed win is a complete cycle award. Monetary fields are minor units; RTP/CI are fractions.

Each cycle has its derived RNG stream. Logical chunks of 4096 cycles are processed in bounded batches of at most 128 chunks. Merge ascending chunk ids, independent of worker scheduling. Memory is bounded by batch size, not sample size. Integer totals are checked exactly. Welford/Chan accumulate variance in complete-cycle stake units; fixed merge order pins floating results.

95% approximate normal CI=mean±1.96×SD/sqrt(PaidCycles). At holdout N=50,000,000, SE=0.001126321587, CI=[0.960344201170,0.964759381790]. Heavy tails and approximate coverage remain limitations; no exact overall RTP or laboratory certification is implied.

Golden seed 42, stream 0, 10,000 frozen cycles: TotalAward=10,110,708; BaseWin=7,133,093; BonusWin=2,977,615; BonusSpins=1030. Separate CLI processes at workers 1/4 agreed exactly on every mathematical count, histogram and CI, excluding performance metadata. Unit tests pin these totals. This small run is a determinism test, not calibration evidence.

Independent exact checks use BigInteger binomial masses and multinomial inclusion-exclusion on initial boards. A two-cell model with equiprobable A/gate, two-A wins and a 3-stake cap has complete-cycle award distribution 0:3/4, 1:3/16, 2:3/64, 3:1/64; expectation=21/64. Enumeration and sampled execution are cross-checked. Property tests cover gravity, all vacancies, identity/removal, evaluator differential behavior, traced/fast equality and event replay.

Reproduce holdout:

```powershell
dotnet run -c Release --project src/Tools/Thunderfall.Simulator.Cli -- --config configs/thunderfall/v1/frozen-v1.json --rounds 50000000 --seed 8675309 --stream 901 --workers 8 --out reports/holdout --label thunderfall-v1-holdout
```

Exact initial checks: `--analytical true --out reports/exact`. Offline real bonus trace: `--trace-feature true --seed 42 --out tests/fixtures`; this is a CLI feature, not an HTTP outcome-control surface. JSON/HTML retain all metrics; CSV contains scalar metrics. Nested histograms/CI are fully preserved in JSON/HTML.
