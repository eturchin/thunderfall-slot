export interface PendingRound {
    sessionId: string;
    stake?: number;
    idempotencyKey: string;
    buyBonus?: boolean;
    balanceBefore?: number;
}

export class PendingRoundStorageError extends Error {
    constructor() {
        super(
            "Could not save the round request. Free browser storage or allow site storage, then retry. No new round was sent.",
        );
    }
}

export function savePendingRound(command: PendingRound): void {
    try {
        localStorage.setItem("tf-pending", JSON.stringify(command));
    } catch {
        throw new PendingRoundStorageError();
    }
}

export function clearPendingRound(): void {
    try {
        localStorage.removeItem("tf-pending");
    } catch {
        // A retained key can only replay the saved operation. The next request overwrites it.
    }
}
