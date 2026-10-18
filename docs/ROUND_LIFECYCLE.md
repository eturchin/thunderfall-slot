# Round lifecycle

A paid cycle is one base round and all its free rounds/retriggers. One HTTP request commits **one** round. One simulation observation completes the entire cycle.

1. Resolve pinned GameId/MathVersion/hash and lock the database session row.
2. Authorize the cookie and resolve committed idempotency before drawing.
3. Infer mode from persisted state; validate stake and base balance.
4. Generate initial board using the correct immutable profile.
5. Evaluate ordinary wins; collect each eligible physical orb once; remove winners together, apply stable gravity/refill; repeat to a terminal state.
6. Resolve triggers/retriggers using initial scatters only. Update a free spin's persistent multiplier before settlement.
7. Calculate one nominal award, round once, apply remaining cycle cap.
8. Produce snapshots, typed mathematical events, next state and separate presentation hints.
9. Atomically persist base debit, award credit, outcome, idempotency, feature state and version, then commit before responding.

Paid Closing=Opening−Stake+FinalWin. Free Closing=Opening+FinalWin. Every wallet mutation has a ledger entry, including initial credit, zero awards and explicit reset. Database constraint forbids negative balance. Checked arithmetic, cancellation and resource failures roll back without a partial payable result.

Example: stake 1,000 minor units (10 credits). Two Crown 8–9 tumbles pay 1.9425× each: raw=3,885 minor units exactly. One physical ×2 orb surviving both contributes once. Final=7,770 (77.70 credits). From 1,000,000 minor units, closing=1,006,770.

Stored results retain MathVersion/hash, starting/final states, original/terminal boards and all real movements/refills/events. Replay does not create another round or mutate a wallet.
