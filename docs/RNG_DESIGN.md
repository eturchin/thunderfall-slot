# RNG design and reproducibility

Production uses RandomNumberGenerator.Fill and rejection sampling. No player seed, stop-selection route or debug RNG exists. Each API request has a scoped stateless crypto source. Mathematical models do not own random state.

Simulation uses xoshiro256** 1.0 seeded by four SplitMix64 outputs. Arithmetic deliberately wraps modulo 2^64. Golden seed 42 begins `15780B2E0C2EC716`, `6104D9866D113A7E`, `AE17533239E499A1`, `ECB8AD4703B360A1`, `FDE6DC7FE2EC5E64`.

Original public-domain references: [xoshiro256**](https://prng.di.unimi.it/xoshiro256starstar.c), David Blackman/Sebastiano Vigna; [SplitMix64](https://prng.di.unimi.it/splitmix64.c), Sebastiano Vigna. Implementation is independently authored; Slot Math Lab was also source-reviewed. PRNG version: `xoshiro256ss-splitmix64-cycle-v1`.

## Pinned derivation

G = 0x9E3779B97F4A7C15. Mix(x) is SplitMix64's xor-shift/multiply avalanche with constants 0xBF58476D1CE4E5B9 and 0x94D049BB133111EB, then x xor (x >> 31).

- domain = little-endian first UInt64 of SHA256(UTF8(GameId + "@" + MathVersion)). Validated identifiers exclude ambiguous separators.
- chunkId = PaidRoundIndex / 4096; offset = PaidRoundIndex % 4096.
- chunkSeed = Mix(masterSeed xor domain xor Mix(StreamId + G) xor Mix(chunkId + 2G)).
- cycleSeed = Mix(chunkSeed + G × (offset + 1)).
- Expand cycleSeed with four successive SplitMix64 words.

All operations above wrap modulo 2^64. One cycle gets an isolated reset stream; all its free spins/retriggers continue that stream. Another cycle cannot consume that state. Workers process fixed logical chunks, merged in ascending chunk order. Scheduling/worker count does not change integer totals, histograms or fixed-order floating reductions. Chunk size and PRNG version are part of the contract.

Avalanched seeding is deterministic domain separation, not proof of disjoint subsequences. Xoshiro is not claimed cryptographically secure and is unavailable in production DI.

## Bounded/weighted sampling

For positive UInt32 bound b, threshold=(2^32−b) mod b. Draw low 32 bits of a uniform UInt64, reject values below threshold, then return word mod b. The accepted space has an exact multiple of b outcomes. Both RNGs implement these semantics, with different sequences.

Integer CDF weights sum in checked UInt32 arithmetic. Binary search selects the first boundary strictly above the draw. Zero-weight entries cannot occur. Orb value is a separate draw after StormOrb selection. Sampling and settlement contain no floating-point probability.

`reports/golden/cross-process-verification.json` records separately launched 1-worker/4-worker comparisons. Tests pin exact payout totals and RNG words. Timing, CPU, allocations and worker metadata are excluded from equality.
