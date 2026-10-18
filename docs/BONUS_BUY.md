# Bonus Buy - storm-buy-1.0.0

The optional entry policy is versioned separately from the unchanged Thunderfall base/free-spin math version 1.0.0. Its authoritative source is `BonusBuyPolicy.cs`; `/games/thunderfall/rules` publishes the policy and exact price ratio to the client. Historical purchase responses persist the policy version, charged cost and awarded-spin count.

## Rules and accounting

- Cost is exactly 455/10 = 45.5 times the selected ordinary stake. All configured stakes give an integral minor-unit cost.
- Entry creates 15 free spins, multiplier 1, zero cycle award. No base board, forced scatters or base-game award is manufactured.
- The purchase request immediately executes the first of those 15 spins. The UI confirmation explicitly states this. Subsequent spins are requested independently, retaining retriggers and accumulated multiplier.
- The original 5000x **ordinary stake** cap covers the purchased bonus cycle. It is not 5000x purchase price.
- Entry and the first free spin, purchase debit, award credit, feature state, complete round record and payload-bound idempotency result commit atomically under the session row lock.
- Repeating a purchase key returns its original result even after the bonus starts. Changing its payload conflicts. A different purchase while a bonus is active is rejected. Classic does not offer this entry.
- Example: stake 20 credits, purchase 910 credits, first award 12 credits: closing balance = opening balance - 910 + 12. Fourteen spins remain unless that spin retriggers or reaches the cycle cap.

## Separate mathematical verification

The RTP denominator is the **purchase price**, never the ordinary stake. Each statistical observation includes the whole purchased bonus with all retriggers. The same `BonusBuyPolicy.Enter` and actual engine are used by the API and simulator. Welford variance is calculated at purchase-cycle level. Stream derivation uses a separate game/version/policy domain.

Price 45.5 was fixed before calibration. Calibration seed 314159: 2,000,000 purchases, RTP 96.1988801923%, approximate 95% CI [95.9849448998%, 96.4128154848%].

Independent holdout seed 271828, stream 902: 10,000,000 purchases, 157,993,924 free rounds. Total purchase cost 455,000,000,000 minor units; total award 438,667,728,186. Observed RTP **96.4104897112%**; approximate 95% CI **[96.3140172651%, 96.5069621573%]**. Price and rules were not changed after the holdout. Its upper CI endpoint exceeds 96.5% by about 0.007 percentage points: the complete design-window criterion is not claimed for Bonus Buy at this precision. The ordinary-game 50-million-cycle report is a different experiment.

Reports: `reports/bonus-buy/{calibration,holdout}.{json,html,csv}`.

```powershell
dotnet run -c Release --project src/Tools/Thunderfall.Simulator.Cli -- --bonus-buy true --rounds 10000000 --seed 271828 --stream 902 --out reports/bonus-buy --label holdout
```

The bonus-buy verification branch is serial; `--workers` applies to the original paid-cycle simulator. Future work is a predeclared larger verification sample or a new candidate policy calibrated independently; do not tune using this holdout.
