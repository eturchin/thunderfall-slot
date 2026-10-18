# Storm multiplier engine

StormOrb stores its category plus an independently sampled value: 2,3,5,8,10,15,20,25,50,100,250 or 500. Integer weights are versioned in each profile. Physical Identity and Collected flag travel unchanged under gravity.

On each positive ordinary-win board, every uncollected orb emits MultiplierCollected and is marked collected. It remains visible but cannot contribute again during that round. New refill orbs get new identities and may activate on later wins. Losing terminal boards do not activate orbs. A forced two-tumble regression proves that a surviving orb is collected once.

Let rawTicks=sum of all cascade coefficients in millionths of total stake, and M=sum of collected values.

- Base effective multiplier=M if M>0, otherwise 1.
- Free effective multiplier=previous persistent multiplier+M, updated before settling and carried onward.

Example: prior bonus multiplier 7, new ×2 and ×3 ⇒ 12. Crown 8–9 at stake 1,000 minor units gives raw=1,942.5 minor units. Nominal award=23,310, not a separately rounded raw value multiplied by twelve.

Settlement uses Int128 products and checked long outputs, rounding once at the entire-round boundary with midpoint-to-even. RawWin is an exact decimal display value; credited FinalWin is a long. Overflow is an explicit rollback failure, never wraparound or hidden clipping. Binary floating point is restricted to statistics/probability display.
