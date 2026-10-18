# RTP calibration and independent holdout

Target 96.0%; allowed design window 95.5–96.5%. No outcomes are changed based on wallet/history. Rules remain immutable during each run.

`simulations/plan.json` reserves calibration seeds 123456/234567 and holdout seed 8675309, stream 901, 50,000,000 cycles. Frozen version/hash and timestamp were recorded **before** holdout. Holdout data was not used to tune weights or table.

Initial table/profile produced about 247% cycle return; a small pilot was insufficiently precise. A five-million-cycle calibration measured its scale, leading to paytable factor 0.3885. Candidate 002 was evaluated with both calibration seeds; version 1.0.0 then received two 20-million-cycle calibration runs. Candidate definitions are immutable files.

| Candidate | Seed | Paid cycles | Observed RTP | 95% cycle CI |
|---|---:|---:|---:|---|
| 1.0.0-candidate-001 | 123456 | 5,000,000 | 247.078638% | [245.333157%,248.824119%] |
| 1.0.0-candidate-002 | 123456 | 5,000,000 | 95.676973% | [95.014300%,96.339646%] |
| 1.0.0-candidate-002 | 234567 | 5,000,000 | 96.191441% | [95.496426%,96.886457%] |
| 1.0.0 | 123456 | 20,000,000 | 96.259560% | [95.905981%,96.613139%] |
| 1.0.0 | 234567 | 20,000,000 | 96.126801% | [95.771638%,96.481965%] |

Independent frozen holdout: **96.255179148%**, CI **[96.034420117%,96.475938179%]**. Half-width=0.220759 percentage points. Both bounds lie inside the design window. Report the estimate as 96.25518%, not an exact 96% claim.

Feature contributions, hit rate, cap-hit frequency, cascade depths and bonus distributions exist for **every** candidate in JSON/HTML. Frozen holdout base=68.386532%, bonus=27.868647%, hit=29.963662%, trigger=0.638052%, cap hits=1. SD=7.964296 stake units; high-volatility behavior is observed, not a certification category.

Author new candidates with `scripts/New-MathCandidate.py`: accepts pay scale, base/bonus symbol weights and orb-value weights, and source definitions permit separate initial/refill profiles. It refuses to overwrite an existing candidate and never reads holdout results. Keep calibration/verification streams separated when extending this process. Thresholds/rules can only change through a newly versioned definition and tests, never during simulation.

Classic was calibrated independently using exact line expectation before a separate five-million-cycle sample; changing Classic did not alter the frozen Thunderfall model or inspect/optimize its holdout.
