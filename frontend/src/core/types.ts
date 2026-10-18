export interface Layout {
    columns: number;
    rows: number;
    size: number;
}
export interface Cell {
    symbol: string;
    multiplier: number;
    identity: number;
    collected: boolean;
}
export interface Board {
    layout: Layout;
    cells: Cell[];
}
export interface Bonus {
    bonusSessionId: string;
    gameId: string;
    mathVersion: string;
    originalStake: number;
    remainingSpins: number;
    playedSpins: number;
    persistentMultiplier: number;
    accumulatedWin: number;
    retriggerCount: number;
    cycleAward: number;
    active: boolean;
}
export interface MathState {
    bonus: Bonus | null;
    isBonus: boolean;
}
export interface Game {
    gameId: string;
    mathVersion: string;
    rulesetHash: string;
    name: string;
    kind: string;
    layout: Layout;
    stakes: number[];
    symbols: string[];
}
export interface Session {
    id: string;
    gameId: string;
    mathVersion: string;
    balance: number;
    featureState: MathState;
    version: number;
}
export interface Group {
    symbol: string;
    count: number;
    payTicks: number;
    positions: number[];
    lineId?: number;
}
export interface Drop {
    from: number;
    to: number;
    cell: Cell;
}
export interface Spawn {
    position: number;
    cell: Cell;
}
export interface Orb {
    position: number;
    identity: number;
    value: number;
}
export interface Step {
    stepIndex: number;
    boardBefore: Board;
    winningGroups: Group[];
    removedPositions: number[];
    gravityMoves: Drop[];
    newSymbols: Spawn[];
    boardAfter: Board;
    baseWin: number;
    multiplierEvents: Orb[];
}
export interface Hint {
    type: string;
    step: number | null;
    position: number | null;
    value: number | null;
}
export interface Outcome {
    roundId: string;
    gameId: string;
    mathVersion: string;
    mode: string;
    stake: number;
    initialBoard: Board;
    cascadeSteps: Step[];
    finalBoard: Board;
    mathematicalEvents: { eventType: string; sequenceNumber: number }[];
    startingMathState: MathState;
    finalMathState: MathState;
    finalWin: number;
    maxWinReached: boolean;
    effectiveMultiplier: number;
    nominalWin: number;
}
export interface Round {
    roundId: string;
    gameId: string;
    mathVersion: string;
    mode: string;
    stake: number;
    initialBoard: Board;
    cascadeSteps: Step[];
    winningGroups: Group[];
    multiplierEvents: Orb[];
    rawWin: number;
    finalWin: number;
    balanceAfter: number;
    balanceBefore?: number;
    stakeDebited?: number;
    balanceAfterDebit?: number;
    totalWin?: number;
    featureState: MathState;
    presentationHints: Hint[];
    outcome: Outcome;
}
export type GamePhase =
    | "Idle" | "Requesting" | "Spinning" | "Revealing" | "Cascading"
    | "ApplyingMultipliers" | "PresentingWin" | "Reconciling" | "Error";
export interface HistoryItem {
    roundId: string;
    gameId: string;
    mathVersion: string;
    mode: string;
    stake: number;
    award: number;
    createdAt: string;
}
export interface Rules {
    bonusBuy?: {
        policyVersion: string;
        priceNumerator: number;
        priceDenominator: number;
        initialSpins: number;
    } | null;
    paytable: Record<string, { minimumCount: number; payTicks: number }[]>;
    maxWinMultiplier: number;
    creditScale: number;
    payScale: number;
    featureRules: { initialSpins: number; retriggerSpins: number };
}
export const credits = (n: number) =>
    new Intl.NumberFormat("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    }).format(n / 100);
