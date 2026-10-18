# Thunderfall mathematics — v1.0.0

RulesetHash `08db994bbd11f3ef89794997af7167182e4b0098424b94cfa9a419f224506506`. Immutable snapshot: `configs/thunderfall/v1/frozen-v1.json`; active `game.json` is identical. Theme assets are separate.

## Layout and distribution

6×5, column-major, row zero top. Eight ordinary paying categories; StormOrb multiplies and ThunderGate triggers. Independent integer-weighted cells/refills, not reel strips.

| Symbol | Base weight | Bonus weight |
|---|---:|---:|
| Crown | 70 | 70 |
| ThunderHammer | 80 | 80 |
| GoldenRing | 90 | 90 |
| PurpleGem | 120 | 120 |
| BlueGem | 130 | 130 |
| GreenGem | 140 | 140 |
| RedGem | 150 | 150 |
| YellowGem | 170 | 170 |
| StormOrb | 25 | 35 |
| ThunderGate | 25 | 15 |

Both totals=1000. Each mode's refill uses explicitly stored identical weights. Orb (value,weight) table: `[(2, 6000), (3, 2000), (5, 1000), (8, 400), (10, 250), (15, 150), (20, 100), (25, 50), (50, 30), (100, 15), (250, 4), (500, 1)]`. Values are independent draws, not symbol-name encodings.

## Frozen paytable

Coefficients apply to total stake. A category pays once per board at the highest applicable count band. No adjacency, paylines or direction requirement.

| Symbol | 8–9 | 10–11 | 12+ |
|---|---:|---:|---:|
| Crown | 1.9425 | 5.8275 | 19.425 |
| ThunderHammer | 1.554 | 4.662 | 13.5975 |
| GoldenRing | 1.1655 | 3.885 | 9.7125 |
| PurpleGem | 0.58275 | 1.554 | 4.662 |
| BlueGem | 0.4662 | 1.1655 | 3.885 |
| GreenGem | 0.3885 | 0.97125 | 3.108 |
| RedGem | 0.3108 | 0.777 | 2.331 |
| YellowGem | 0.19425 | 0.58275 | 1.9425 |

The supplied initial table was scaled by 0.3885 using calibration data only. Candidates/reports are retained. These are independently designed demo rules, not commercial proprietary mathematics.

## Settlement

Coefficients are exact millionth-stake integer ticks. Stake/wallet/awards are long minor units (100=1 credit). Sum raw cascade ticks, apply the round multiplier, round once to nearest minor unit (ties-even), then apply remaining paid-cycle cap. Int128 intermediates/checked long casts reject overflow. Work exhaustion, overflow or cancellation fails and rolls back.

Base multiplier=sum of eligible collected orb values, or one. In free mode add new values to persistent multiplier (starting at one) before settling. Stable physical identity prevents double collection. Four initial base gates award 15 spins; three initial bonus gates award five more. All related rounds share original stake and 5000× cap. See the dedicated cascade/multiplier/bonus documents for concrete examples and termination arguments.

## Independent checks and limits of inference

Exact checks cover initial symbol-count binomials, a multinomial inclusion-exclusion union for any ordinary 8+ win, scatter triggers and orb occurrence. These are initial-cell calculations only. Tumble survivors create dependence: no exact overall RTP is claimed from a single-board formula. A tiny two-cell complete-cycle model is checked by absorbing enumeration; an independent reference evaluator checks optimized counting.

Holdout: 96.255179148% RTP; 95% approximate cycle CI [96.034420117%,96.475938179%]. Base 68.386532384%, bonus 27.868646764%. This supports the 95.5–96.5% design window at measured precision. It is not exact 96.000%, certification or a short-session guarantee.

## Classic

Separate 5×3 cyclic strips, five fixed paths, best Wild substitution, no bonus. Top row is chosen stop; lower rows wrap forward. Its table was scaled from independent enumeration of all 9^5 line sequences. Final 5,000,000-cycle sample: 96.143501% (CI in `reports/classic/classic-final.json`). It proves shared engine/platform reuse.
