import { useEffect, useRef, useState } from "react";
import { gsap } from "gsap";
import { api, ApiError } from "../api/client";
import type { Game, Session, Round, Rules, HistoryItem, GamePhase } from "../core/types";
import { credits } from "../core/types";
import {
    clearPendingRound,
    savePendingRound,
    PendingRoundStorageError,
    type PendingRound,
} from "../core/pendingRound";
import { AudioManager } from "../audio/AudioManager";
import { GameCanvas } from "../components/GameCanvas";
import type { SceneHandle } from "../components/GameCanvas";
import { Dialog } from "../components/Dialog";
import styles from "./App.module.css";
function readSaved<T>(key: string, fallback: T): T {
    try {
        return JSON.parse(localStorage.getItem(key) ?? "null") ?? fallback;
    } catch {
        return fallback;
    }
}
function payoutMinor(stake: number, ticks: number, scale: number): bigint {
    const numerator = BigInt(stake) * BigInt(ticks);
    const denominator = BigInt(scale);
    const quotient = numerator / denominator;
    const remainder = numerator % denominator;
    const twice = remainder * 2n;
    return twice > denominator || (twice === denominator && quotient % 2n !== 0n)
        ? quotient + 1n
        : quotient;
}
const errors: Record<string, string> = {
    InsufficientBalance:
        "Not enough virtual credits. Restore your demo balance to continue.",
    InvalidStake: "Choose one of the available stakes.",
    IdempotencyConflict:
        "This request conflicts with an earlier round. Reload to reconcile the session.",
    SessionNotFound: "Your session has expired. Reload to start a new demo.",
};
export default function App() {
    const [games, setGames] = useState<Game[]>([]),
        [game, setGame] = useState<Game>(),
        [rules, setRules] = useState<Rules>(),
        [session, setSession] = useState<Session>(),
        [displayedBalance, setDisplayedBalance] = useState(0),
        [round, setRound] = useState<Round>(),
        [stake, setStake] = useState(2000),
        [ready, setReady] = useState(false),
        [busy, setBusy] = useState(false),
        [phase, setPhase] = useState<GamePhase>("Idle"),
        [error, setError] = useState(""),
        [status, setStatus] = useState("Awakening the storm…");
    const [turbo, setTurbo] = useState(() => readSaved("tf-turbo", false)),
        [displayWin, setDisplayWin] = useState(0),
        [activeStep, setActiveStep] = useState(-1),
        [modal, setModal] = useState<
            "rules" | "sound" | "history" | "reset" | "bonus" | null
        >(null),
        [history, setHistory] = useState<HistoryItem[]>([]);
    const [historyPage, setHistoryPage] = useState(1),
        [historyTotal, setHistoryTotal] = useState(0),
        [historyLoading, setHistoryLoading] = useState(false),
        [historyError, setHistoryError] = useState("");
    const historyRequest = useRef(0);
    const gameWindow = useRef<HTMLElement>(null);
    const [audio] = useState(() => new AudioManager()),
        [sound, setSound] = useState(audio.settings);
    const sceneReady = useRef<(() => void) | null>(null);
    const scene = useRef<SceneHandle>(null),
        last = useRef<Round | undefined>(undefined),
        pending = useRef<PendingRound | undefined>(
            readSaved<PendingRound | undefined>("tf-pending", undefined),
        ),
        locked = useRef(false),
        alive = useRef(true),
        winTween = useRef<gsap.core.Tween | null>(null);
    useEffect(() => {
        alive.current = true;
        void (async () => {
            try {
                const list = await api.games();
                let s: Session;
                try {
                    s = await api.current();
                } catch (e) {
                    if (e instanceof ApiError && e.status === 404)
                        s = await api.create("thunderfall");
                    else throw e;
                }
                let recovered: Round | undefined;
                let recoveryError = "";
                if (pending.current && pending.current.sessionId !== s.id) {
                    pending.current = undefined;
                    clearPendingRound();
                }
                if (pending.current?.sessionId === s.id) {
                    try {
                        const command = pending.current;
                        recovered = await api.round({
                            stake: command.stake,
                            idempotencyKey: command.idempotencyKey,
                            ...(command.buyBonus ? { buyBonus: true } : {}),
                        });
                        try {
                            s = await api.current();
                            pending.current = undefined;
                            clearPendingRound();
                        } catch {
                            s = { ...s, balance: recovered.balanceAfter, featureState: recovered.featureState };
                            recoveryError = "Round recovered. Confirming the latest wallet state.";
                        }
                    } catch (e) {
                        recoveryError = "Resolving the saved round request. Retry will use the same request.";
                        if (e instanceof ApiError && [400, 403, 404, 409, 422].includes(e.status)) {
                            pending.current = undefined;
                            clearPendingRound();
                        }
                    }
                }
                let g = list.find((x) => x.gameId === s.gameId);
                if (!g) throw new Error("Unknown game");
                if (g.mathVersion !== s.mathVersion)
                    g = await api.game(s.gameId, s.mathVersion);
                const [r, h] = await Promise.all([
                    api.rules(s.gameId, s.mathVersion),
                    api.history(),
                ]);
                const latest = recovered ?? (
                    h[0]?.gameId === s.gameId
                        ? await api.replay(h[0].roundId)
                        : undefined);
                if (!alive.current) return;
                last.current = latest;
                setRound(latest);
                setDisplayWin(latest?.finalWin ?? 0);
                setSession(s);
                setDisplayedBalance(s.balance);
                setGames(list);
                setRules(r);
                setGame(g);
                setStake(
                    s.featureState.isBonus
                        ? s.featureState.bonus!.originalStake
                        : 2000,
                );
                setStatus(
                    s.featureState.isBonus
                        ? "Your free spins are waiting."
                        : "The storm is yours to summon.",
                );
                if (recoveryError) setError(recoveryError);
            } catch (e) {
                if (alive.current)
                    setError(
                        e instanceof ApiError
                            ? (errors[e.code] ?? e.code)
                            : "The forge is unreachable. Start the API and reload.",
                    );
            }
        })();
        return () => {
            alive.current = false;
            winTween.current?.kill();
            audio.dispose();
        };
    }, [audio]);
    const updateSound = (patch: Partial<typeof sound>) => {
        void audio.unlock();
        audio.configure(patch);
        setSound({ ...audio.settings });
    };
    const stage = (step: number, r: Round) => {
        setPhase("Cascading");
        setActiveStep(step);
        setStatus(
            `Tumble ${step + 1} · ${r.cascadeSteps[step].winningGroups.map((g) => `${g.count} ${g.symbol.replace(/([a-z])([A-Z])/g, "$1 $2")}`).join(" + ")}`,
        );
    };
    async function spin(buyBonus = false) {
        if (locked.current || !session || !game || !ready) return;
        locked.current = true;
        setBusy(true);
        setPhase("Requesting");
        setError("");
        setDisplayWin(0);
        setActiveStep(-1);
        setStatus("The sky answers…");
        let roundAccepted = false;
        let failed = false;
        let optimisticBalance: number | undefined;
        try {
            let command = pending.current;
            const retryingPending = !!command && command.sessionId === session.id;
            if (!command || !retryingPending) {
                command = {
                    sessionId: session.id,
                    balanceBefore: session.balance,
                    ...(session.featureState.isBonus ? {} : { stake }),
                    idempotencyKey: crypto.randomUUID(),
                    ...(buyBonus ? { buyBonus: true } : {}),
                };
                pending.current = command;
            }
            savePendingRound(command);
            const roundStake = command.stake ?? stake;
            const freeSpin = !command.buyBonus && command.stake === undefined;
            const debit = freeSpin
                ? 0
                : command.buyBonus
                  ? Math.floor(
                        (roundStake *
                            (rules?.bonusBuy?.priceNumerator ?? 1)) /
                            (rules?.bonusBuy?.priceDenominator ?? 1),
                    )
                  : roundStake;
            if (!retryingPending && session.balance < debit)
                throw new ApiError("InsufficientBalance", 409);
            optimisticBalance = Math.max(
                0,
                (command.balanceBefore ?? session.balance) - debit,
            );
            setDisplayedBalance(optimisticBalance);
            void audio.unlock();
            audio.play("button_click");
            scene.current?.begin(turbo);
            setPhase("Spinning");
            const r = await api.round({
                stake: command.stake,
                idempotencyKey: command.idempotencyKey,
                ...(command.buyBonus ? { buyBonus: true } : {}),
            });
            roundAccepted = true;
            setPhase("Revealing");
            if (!alive.current) return;
            await scene.current?.play(r, turbo, stage, setPhase);
            if (!alive.current) return;
            last.current = r;
            setRound(r);
            const message = r.outcome.maxWinReached
                ? "MAXIMUM WIN · 5,000× cycle complete"
                : r.presentationHints.some((h) => h.type === "BonusIntro")
                  ? "The gates are open. 15 free spins awarded."
                  : r.presentationHints.some((h) => h.type === "BonusComplete")
                    ? "The storm rests. Bonus complete."
                    : r.featureState.isBonus
                      ? `${r.featureState.bonus!.remainingSpins} free spins remain · the multiplier carries on`
                      : r.finalWin > 0
                        ? "A gift from the Titans."
                        : "The storm gathers for another round.";
            setStatus(message);
            const value = { win: 0 };
            setPhase("PresentingWin");
            winTween.current?.kill();
            await new Promise<void>((resolve) => {
                winTween.current = gsap.to(value, {
                    win: r.finalWin,
                    duration: matchMedia("(prefers-reduced-motion: reduce)").matches
                        ? 0
                        : turbo
                          ? 0.35
                          : Math.min(2, 0.65 + r.finalWin / Math.max(roundStake * 20, 1) * 0.2),
                    onUpdate: () => {
                        setDisplayWin(Math.round(value.win));
                        setDisplayedBalance(
                            Math.min(r.balanceAfter, (optimisticBalance ?? r.balanceAfter) + Math.round(value.win)),
                        );
                    },
                    onComplete: () => {
                        setDisplayWin(r.finalWin);
                        setDisplayedBalance(r.balanceAfter);
                        resolve();
                    },
                });
            });
            setPhase("Reconciling");
            const authoritative = await api.current();
            if (!alive.current) return;
            setSession(authoritative);
            setDisplayedBalance(authoritative.balance);
            pending.current = undefined;
            clearPendingRound();
        } catch (e) {
            if (!alive.current) return;
            failed = true;
            setPhase("Error");
            scene.current?.restore(last.current);
            if (optimisticBalance !== undefined) {
                try {
                    const confirmed = await api.current();
                    setSession(confirmed);
                    setDisplayedBalance(confirmed.balance);
                } catch {
                    setDisplayedBalance(session.balance);
                }
            }
            setError(
                e instanceof PendingRoundStorageError
                    ? e.message
                    : e instanceof ApiError
                      ? (errors[e.code] ?? e.code)
                      : "Connection interrupted. Retry resolves the same saved request.",
            );
            setStatus("Your last confirmed balance is shown.");
            if (
                !roundAccepted &&
                e instanceof ApiError &&
                [400, 403, 404, 409, 422].includes(e.status)
            ) {
                pending.current = undefined;
                clearPendingRound();
            }
        } finally {
            if (alive.current) {
                if (!failed) setPhase("Idle");
                locked.current = false;
                setBusy(false);
            }
        }
    }
    async function replay(id?: string) {
        if (locked.current || !ready) return;
        locked.current = true;
        setBusy(true);
        setModal(null);
        try {
            const r = id ? await api.replay(id) : round;
            if (!r) return;
            if (
                r.gameId !== game?.gameId ||
                r.mathVersion !== game?.mathVersion
            ) {
                if (session?.featureState.isBonus)
                    throw new Error(
                        "Finish the bonus before switching replay games",
                    );
                const target = await api.game(r.gameId, r.mathVersion);
                const switched = await api.switchGame(r.gameId);
                setSession(switched);
                setRules(await api.rules(r.gameId, r.mathVersion));
                last.current = undefined;
                setReady(false);
                await new Promise<void>((resolve, reject) => {
                    const timer = window.setTimeout(() => {
                        sceneReady.current = null;
                        reject(new Error("Renderer timeout"));
                    }, 10000);
                    sceneReady.current = () => {
                        window.clearTimeout(timer);
                        resolve();
                    };
                    setGame(target);
                });
            }
            setStatus("Replaying a recorded round…");
            scene.current?.begin(turbo);
            await scene.current?.play(r, turbo, stage);
            setRound(r);
            setDisplayWin(r.finalWin);
            setStatus("Replay complete · your balance is unchanged.");
        } catch {
            setError("Could not load this replay.");
        } finally {
            locked.current = false;
            setBusy(false);
        }
    }
    async function changeGame(id: string) {
        if (locked.current) return;
        locked.current = true;
        setBusy(true);
        setError("");
        try {
            const s = await api.switchGame(id),
                r = await api.rules(id);
            pending.current = undefined;
            clearPendingRound();
            last.current = undefined;
            setRound(undefined);
            setDisplayWin(0);
            setActiveStep(-1);
            setReady(false);
            setSession(s);
            setRules(r);
            setGame(games.find((g) => g.gameId === id));
            setStatus("A new legend begins.");
        } catch {
            setError("Could not start this game.");
        } finally {
            locked.current = false;
            setBusy(false);
        }
    }
    async function showHistory(page = 1) {
        if (page === 1) setHistoryPage(1);
        setModal("history");
        setHistoryLoading(true);
        setHistoryError("");
        const request = ++historyRequest.current;
        try {
            const result = await api.historyPage(page);
            if (request !== historyRequest.current || !alive.current) return;
            setHistory(result.items);
            setHistoryTotal(result.total);
            setHistoryPage(result.page);
        } catch {
            if (request === historyRequest.current)
                setHistoryError("History could not be loaded. Try again.");
        } finally {
            if (request === historyRequest.current) setHistoryLoading(false);
        }
    }
    async function fullscreen() {
        try {
            if (document.fullscreenElement) await document.exitFullscreen();
            else await gameWindow.current?.requestFullscreen();
        } catch {
            setError("Fullscreen is unavailable in this browser.");
        }
    }
    async function reset() {
        if (locked.current) return;
        locked.current = true;
        setBusy(true);
        try {
            setSession(await api.reset());
            pending.current = undefined;
            clearPendingRound();
            setRound(undefined);
            last.current = undefined;
            setDisplayWin(0);
            setActiveStep(-1);
            setModal(null);
            setError("");
            setStatus("10,000 virtual credits restored.");
        } catch {
            setError("Could not restore the demo balance.");
        } finally {
            locked.current = false;
            setBusy(false);
        }
    }
    const bonus = session?.featureState.isBonus ?? false;
    const multiplier = bonus
        ? session!.featureState.bonus!.persistentMultiplier
        : (round?.outcome.effectiveMultiplier ?? 1);
    const currentStake = bonus
        ? session!.featureState.bonus!.originalStake
        : stake;
    const buyMultiple = rules?.bonusBuy
        ? rules.bonusBuy.priceNumerator / rules.bonusBuy.priceDenominator
        : 0;
    const buyCost = currentStake * buyMultiple;
    const hasPending =
        !!pending.current && pending.current.sessionId === session?.id;
    const thunder = game?.kind === "CountAnywhere";
    const paytableBands = Object.values(rules?.paytable ?? {})[0] ?? [];
    const paytableColumns = paytableBands.map((band, index) => {
        if (!thunder) return String(band.minimumCount);
        const next = paytableBands[index + 1]?.minimumCount;
        return next
            ? `${band.minimumCount}–${next - 1}`
            : `${band.minimumCount}+`;
    });
    return (
        <div className={`${styles.app} ${bonus ? styles.bonus : ""}`} data-game-phase={phase}>
            <header className={styles.siteHeader}>
                <a href="/" className={styles.siteBrand}>
                    STORM<span>ARCADE</span>
                </a>
                <span className={styles.siteTagline}>
                    Original games. Virtual credits. Endless atmosphere.
                </span>
                <span className={styles.siteBadge}>DEMO LOBBY</span>
            </header>
            <div className={styles.breadcrumb}>
                Arcade <span>/</span> Slots <span>/</span>{" "}
                {game?.name ?? "Thunderfall"}
            </div>
            <main
                ref={gameWindow}
                className={styles.gameWindow}
                aria-label="Game window"
            >
                <header className={styles.topbar}>
                    <a href="/" className={styles.brand}>
                        <Icon name="bolt" />
                        <span>
                            THUNDERFALL<span>ORIGINAL GAME SERIES</span>
                        </span>
                    </a>
                    <span className={styles.demo}>VIRTUAL CREDITS ONLY</span>
                    <nav aria-label="Game controls">
                        <button
                            aria-label={
                                sound.muted ? "Unmute sound" : "Mute sound"
                            }
                            aria-pressed={!sound.muted}
                            onClick={() => updateSound({ muted: !sound.muted })}
                        >
                            <Icon name={sound.muted ? "mute" : "sound"} />
                        </button>
                        <button
                            aria-label="Sound settings"
                            onClick={() => setModal("sound")}
                        >
                            <Icon name="settings" />
                        </button>
                        <button
                            aria-label="Game rules"
                            onClick={() => setModal("rules")}
                        >
                            <Icon name="info" />
                        </button>
                        <button
                            aria-label="Round history"
                            onClick={() => void showHistory()}
                        >
                            <Icon name="history" />
                        </button>
                        <button
                            aria-label="Fullscreen"
                            onClick={() => void fullscreen()}
                        >
                            <Icon name="expand" />
                        </button>
                    </nav>
                </header>
                <div className={styles.gameContent}>
                    <div className={styles.kicker}>
                        <span /> WIN UP TO 5,000× YOUR STAKE <span />
                    </div>
                    <div className={styles.arena}>
                        <aside className={styles.left}>
                            {thunder && rules?.bonusBuy && (
                                <button
                                    className={styles.bonusButton}
                                    onClick={() => setModal("bonus")}
                                >
                                    <span>
                                        {rules.bonusBuy.initialSpins} FREE SPINS
                                    </span>
                                    <strong>BUY BONUS</strong>
                                    <b>{credits(buyCost)}</b>
                                    <small>
                                        {rules?.bonusBuy?.initialSpins} free
                                        spins / {buyMultiple}x stake
                                    </small>
                                </button>
                            )}
                            <div className={styles.stormSeal}>
                                <span>STORM MULTIPLIER</span>
                                <strong data-testid="multiplier">
                                    ×{multiplier}
                                </strong>
                                <small>
                                    {bonus
                                        ? "PERSISTENT BONUS POWER"
                                        : "AWAKENED BY WINNING TUMBLES"}
                                </small>
                                <Icon name="bolt" />
                            </div>
                            <div className={styles.legend}>
                                <img
                                    src="/assets/symbols/StormOrb.svg"
                                    alt=""
                                />
                                <h3>Harness the storm</h3>
                                <p>
                                    {thunder
                                        ? "Orbs ignite on winning tumbles. Their power joins your final award."
                                        : "Wilds replace ordinary symbols along each winning path."}
                                </p>
                            </div>
                            <div className={styles.maxWin}>
                                <span>MAXIMUM CYCLE WIN</span>
                                <b>5,000×</b>
                                <small>THE POWER OF A TITAN</small>
                            </div>
                        </aside>
                        <section
                            className={styles.machine}
                            aria-label="Slot game"
                        >
                            <div className={styles.machineTop}>
                                <span>
                                    {thunder
                                        ? "6 × 5 · TUMBLE PAYS"
                                        : "5 × 3 · FIXED PAYLINES"}
                                </span>
                                <span>
                                    {bonus
                                        ? `FREE SPINS · ${session!.featureState.bonus!.remainingSpins} REMAINING`
                                        : thunder
                                          ? "8+ MATCHING SYMBOLS PAY ANYWHERE"
                                          : "MATCH 3 OR MORE LEFT TO RIGHT"}
                                </span>
                            </div>
                            <div className={styles.frame}>
                                <i />
                                <i />
                                <i />
                                <i />
                                {game ? (
                                    <GameCanvas
                                        key={game.gameId}
                                        game={game}
                                        audio={audio}
                                        ref={scene}
                                        onReady={() => {
                                            setReady(true);
                                            sceneReady.current?.();
                                            sceneReady.current = null;
                                            if (last.current)
                                                scene.current?.restore(
                                                    last.current,
                                                );
                                        }}
                                    />
                                ) : (
                                    <div className={styles.loading}>
                                        Summoning the Titans…
                                    </div>
                                )}
                                {round?.presentationHints.some(
                                    (h) => h.type === "BonusIntro",
                                ) &&
                                    !busy &&
                                    bonus && (
                                        <div className={styles.bonusIntro}>
                                            <span>THE GATES ARE OPEN</span>
                                            <strong>15 FREE SPINS</strong>
                                            <small>
                                                YOUR MULTIPLIER CARRIES THROUGH
                                                THE STORM
                                            </small>
                                        </div>
                                    )}
                                {round &&
                                    round.finalWin >= round.stake * 20 &&
                                    !busy && (
                                        <div className={styles.bigWin}>
                                            {round.finalWin >= round.stake * 100
                                                ? "MEGA WIN"
                                                : "BIG WIN"}
                                        </div>
                                    )}
                            </div>
                            <div
                                className={styles.status}
                                role="status"
                                aria-live="polite"
                            >
                                {status}
                            </div>
                            <div className={styles.mobileMetrics}>
                                <div>
                                    <span>ROUND WIN</span>
                                    <strong data-testid="mobile-win">
                                        {credits(displayWin)}
                                    </strong>
                                </div>
                                <div>
                                    <span>
                                        {thunder
                                            ? "STORM POWER"
                                            : "WINNING PATHS"}
                                    </span>
                                    <strong>
                                        {thunder
                                            ? "\u00d7" + multiplier
                                            : (round?.winningGroups.length ??
                                              0)}
                                    </strong>
                                </div>
                                <div>
                                    <span>
                                        {bonus ? "FREE SPINS" : "TUMBLES"}
                                    </span>
                                    <strong>
                                        {bonus
                                            ? session!.featureState.bonus!
                                                  .remainingSpins
                                            : (round?.cascadeSteps.length ?? 0)}
                                    </strong>
                                </div>
                            </div>
                            <div className={styles.controls}>
                                <div>
                                    <span>BALANCE</span>
                                    <strong data-testid="balance">
                                        {credits(displayedBalance)}
                                    </strong>
                                    <small>VIRTUAL CREDITS</small>
                                </div>
                                <div className={styles.stake}>
                                    <span>TOTAL STAKE</span>
                                    <div>
                                        <button
                                            aria-label="Decrease stake"
                                            disabled={
                                                busy ||
                                                bonus ||
                                                !game ||
                                                stake === game.stakes[0]
                                            }
                                            onClick={() =>
                                                setStake(
                                                    game!.stakes[
                                                        game!.stakes.indexOf(
                                                            stake,
                                                        ) - 1
                                                    ],
                                                )
                                            }
                                        >
                                            −
                                        </button>
                                        <strong data-testid="stake">
                                            {credits(currentStake)}
                                        </strong>
                                        <button
                                            aria-label="Increase stake"
                                            disabled={
                                                busy ||
                                                bonus ||
                                                !game ||
                                                stake === game.stakes.at(-1)
                                            }
                                            onClick={() =>
                                                setStake(
                                                    game!.stakes[
                                                        game!.stakes.indexOf(
                                                            stake,
                                                        ) + 1
                                                    ],
                                                )
                                            }
                                        >
                                            +
                                        </button>
                                    </div>
                                    <small>
                                        {bonus
                                            ? "TRIGGER STAKE LOCKED"
                                            : "ALL POSITIONS ACTIVE"}
                                    </small>
                                </div>
                                <button
                                    className={styles.spin}
                                    aria-label={
                                        busy
                                            ? "Skip animation"
                                            : hasPending
                                              ? "Retry round"
                                              : bonus
                                                ? "Free spin"
                                                : "Spin"
                                    }
                                    disabled={!session || !ready}
                                    onClick={() =>
                                        busy
                                            ? scene.current?.skip()
                                            : void spin()
                                    }
                                >
                                    <Icon name={busy ? "skip" : "spin"} />
                                    <span>
                                        {busy
                                            ? "SKIP"
                                            : hasPending
                                              ? "RETRY"
                                              : bonus
                                                ? "FREE SPIN"
                                                : "SPIN"}
                                    </span>
                                </button>
                            </div>
                            <div className={styles.utility}>
                                <button
                                    aria-pressed={turbo}
                                    onClick={() => {
                                        setTurbo(!turbo);
                                        localStorage.setItem(
                                            "tf-turbo",
                                            JSON.stringify(!turbo),
                                        );
                                    }}
                                >
                                    <Icon name="bolt" /> TURBO{" "}
                                    <b>{turbo ? "ON" : "OFF"}</b>
                                </button>
                                <button
                                    disabled={busy || !round}
                                    onClick={() => void replay()}
                                >
                                    <Icon name="replay" /> REPLAY ROUND
                                </button>
                                <button
                                    disabled={busy || hasPending}
                                    onClick={() => setModal("reset")}
                                >
                                    RESTORE CREDITS
                                </button>
                            </div>
                        </section>
                        <aside className={styles.right}>
                            <div className={styles.gameTitle}>
                                <span>WRATH OF THE TITANS</span>
                                <h1>
                                    THUNDER
                                    <br />
                                    FALL
                                </h1>
                                <p className={styles.subtitle}>
                                    {thunder
                                        ? "WINTER OLYMPUS"
                                        : "CLASSIC LEGENDS"}
                                </p>
                            </div>
                            <div className={styles.winPanel}>
                                <span>ROUND WIN</span>
                                <strong data-testid="win">
                                    {credits(displayWin)}
                                </strong>
                                <small>VIRTUAL CREDITS</small>
                                <div className={styles.divider} />
                                <span>
                                    {bonus ? "BONUS PROGRESS" : "TUMBLE CHAIN"}
                                </span>
                                <b>
                                    {bonus
                                        ? `${session!.featureState.bonus!.remainingSpins} SPINS`
                                        : round
                                          ? `${round.cascadeSteps.length} TUMBLES`
                                          : "AWAITING THE STORM"}
                                </b>
                                {bonus && (
                                    <p>
                                        Bonus total{" "}
                                        <em>
                                            {credits(
                                                session!.featureState.bonus!
                                                    .accumulatedWin,
                                            )}
                                        </em>
                                    </p>
                                )}
                            </div>
                            <div className={styles.gateCard}>
                                <img
                                    src="/assets/symbols/ThunderGate.svg"
                                    alt=""
                                />
                                <h3>
                                    {bonus
                                        ? "THE STORM CONTINUES"
                                        : thunder
                                          ? "OPEN THE GATES"
                                          : "CLASSIC LEGENDS"}
                                </h3>
                                <p>
                                    {thunder
                                        ? "4 gates unlock 15 free spins. 3 gates in the bonus award 5 more."
                                        : "Five reels. Five winning paths. The same Thunderfall engine."}
                                </p>
                            </div>
                            <div className={styles.roundList}>
                                {round?.cascadeSteps.slice(0, 5).map((s) => (
                                    <div
                                        className={
                                            activeStep === s.stepIndex
                                                ? styles.activeStep
                                                : ""
                                        }
                                        key={s.stepIndex}
                                    >
                                        <span>TUMBLE {s.stepIndex + 1}</span>
                                        <strong>+{credits(s.baseWin)}</strong>
                                    </div>
                                ))}
                            </div>
                        </aside>
                    </div>
                    {error && (
                        <div className={styles.error} role="alert">
                            {error}
                        </div>
                    )}
                    <footer>
                        <span>
                            THUNDERFALL <b>/</b> WINTER OLYMPUS
                        </span>
                        <label>
                            EXPLORE{" "}
                            <select
                                aria-label="Choose game"
                                disabled={busy || bonus || hasPending}
                                value={game?.gameId ?? "thunderfall"}
                                onChange={(e) =>
                                    void changeGame(e.target.value)
                                }
                            >
                                {(games.length
                                    ? games
                                    : [
                                          {
                                              gameId: "thunderfall",
                                              name: "Wrath of the Titans",
                                          },
                                      ]
                                ).map((g) => (
                                    <option key={g.gameId} value={g.gameId}>
                                        {g.name}
                                    </option>
                                ))}
                            </select>
                        </label>
                        <small>Demo only. No real money. No cash value.</small>
                    </footer>
                </div>
                {modal === "bonus" && (
                    <Dialog
                        title="ThunderGate bonus"
                        onClose={() => setModal(null)}
                    >
                        <div className={styles.bonusCard}>
                            <img src="/assets/symbols/ThunderGate.svg" alt="" />
                            <h3>Awaken 15 free spins</h3>
                            <p>
                                Land 4 or more ThunderGates on the initial
                                board. During free spins, 3 or more gates award
                                5 extra spins. Your storm multiplier carries
                                through the bonus.
                            </p>
                            <p>
                                Cost:{" "}
                                <strong>
                                    {credits(buyCost)} virtual credits
                                </strong>{" "}
                                ({buyMultiple}x stake). The purchase immediately
                                plays the first of 15 free spins. Retriggers and
                                the 5,000x original-stake cycle cap apply.
                                Separate Bonus Buy policy:{" "}
                                {rules?.bonusBuy?.policyVersion}.
                            </p>
                            <button
                                disabled={
                                    busy ||
                                    bonus ||
                                    !rules?.bonusBuy ||
                                    hasPending ||
                                    !ready ||
                                    !session ||
                                    session.balance < buyCost
                                }
                                onClick={() => {
                                    setModal(null);
                                    void spin(true);
                                }}
                            >
                                Buy & play first free spin
                            </button>
                            <p>
                                {bonus
                                    ? "Finish the active bonus before buying another."
                                    : session && session.balance < buyCost
                                      ? "Not enough virtual credits. Lower your stake or restore demo credits."
                                      : "Virtual credits only. No cash value."}
                            </p>
                        </div>
                    </Dialog>
                )}
                {modal === "rules" && (
                    <Dialog
                        title="Rules & paytable"
                        onClose={() => setModal(null)}
                    >
                        <p>
                            {thunder
                                ? "8 or more matching ordinary symbols anywhere on the 6×5 board pay once per symbol. All winning symbols disappear together, survivors fall, and the board refills until no wins remain."
                                : "Five fixed paylines pay the best 3, 4 or 5 matching symbols from the left. Wild substitutes ordinary symbols."}
                        </p>
                        <p>Payouts for the selected total bet: {credits(stake)} credits.</p>
                        <div className={styles.paytable}>
                            <div>
                                <span>SYMBOL</span>
                                {paytableColumns.map((x) => (
                                    <b key={x}>{x}</b>
                                ))}
                            </div>
                            {Object.entries(rules?.paytable ?? {}).map(
                                ([symbol, bands]) => (
                                    <div key={symbol}>
                                        <span>
                                            <img
                                                src={
                                                    "/assets/symbols/" +
                                                    symbol +
                                                    ".svg"
                                                }
                                                alt=""
                                            />
                                            {symbol.replace(
                                                /([a-z])([A-Z])/g,
                                                "$1 $2",
                                            )}
                                        </span>
                                        {bands.map((b) => (
                                            <b key={b.minimumCount}>
                                                {credits(Number(payoutMinor(stake, b.payTicks, rules?.payScale ?? 1)))}
                                            </b>
                                        ))}
                                    </div>
                                ),
                            )}
                        </div>
                        <p>Payouts use the active server math version and selected total bet. Final wins can increase through multipliers and cascades.</p>
                        {thunder && (
                            <>
                                <p>
                                    <b>Storm Orbs</b> activate only on winning
                                    boards. Each physical orb contributes once
                                    per round, even if it survives multiple
                                    tumbles. Collected orb values are added
                                    together and multiply the total raw tumble
                                    award.
                                </p>
                                <p>
                                    <b>Free spins:</b> 4+ gates on the initial
                                    paid board award 15 spins. In a free spin,
                                    3+ gates on the initial board award 5 more.
                                    The multiplier starts at ×1 and every
                                    collected value adds to it before that free
                                    spin settles.
                                </p>
                            </>
                        )}
                        <p>
                            The complete paid cycle, including all free spins,
                            can award at most 5,000× its original stake.
                            Reaching that cap ends the bonus. Awards round once
                            to the nearest 0.01 credit, ties to even. Virtual
                            credits have no monetary value.
                        </p>
                    </Dialog>
                )}
                {modal === "sound" && (
                    <Dialog
                        title="Sound settings"
                        onClose={() => setModal(null)}
                    >
                        <label className={styles.setting}>
                            Enable sound
                            <input
                                type="checkbox"
                                checked={!sound.muted}
                                onChange={(e) =>
                                    updateSound({ muted: !e.target.checked })
                                }
                            />
                        </label>
                        <label className={styles.setting}>
                            Ambient music
                            <input
                                type="range"
                                aria-label="Music volume"
                                min="0"
                                max="1"
                                step=".01"
                                value={sound.music}
                                onChange={(e) =>
                                    updateSound({
                                        music: Number(e.target.value),
                                    })
                                }
                            />
                        </label>
                        <label className={styles.setting}>
                            Sound effects
                            <input
                                type="range"
                                aria-label="Effects volume"
                                min="0"
                                max="1"
                                step=".01"
                                value={sound.sfx}
                                onChange={(e) =>
                                    updateSound({ sfx: Number(e.target.value) })
                                }
                            />
                        </label>
                        <p>
                            Original synthesized audio. Your preferences stay on
                            this device.
                        </p>
                    </Dialog>
                )}
                {modal === "reset" && (
                    <Dialog
                        title="Restore demo credits"
                        onClose={() => setModal(null)}
                    >
                        <p>
                            Restore 10,000 virtual credits. This ends any active
                            bonus and records a reset in your wallet ledger.
                        </p>
                        <button
                            className={styles.confirm}
                            disabled={busy}
                            onClick={() => void reset()}
                        >
                            Restore 10,000 credits
                        </button>
                    </Dialog>
                )}
                {modal === "history" && (
                    <Dialog
                        title="Round history"
                        onClose={() => setModal(null)}
                    >
                        <p>
                            All {historyTotal.toLocaleString()} committed rounds
                            are retained in PostgreSQL for this session,
                            including both games. Replays never change your
                            wallet.
                        </p>
                        <div
                            className={styles.pagination}
                            aria-label="History pagination"
                        >
                            <button
                                disabled={historyLoading || historyPage === 1}
                                onClick={() =>
                                    void showHistory(historyPage - 1)
                                }
                            >
                                Previous page
                            </button>
                            <span aria-live="polite">
                                Page {historyPage} of{" "}
                                {Math.max(1, Math.ceil(historyTotal / 20))}
                            </span>
                            <button
                                disabled={
                                    historyLoading ||
                                    historyPage * 20 >= historyTotal
                                }
                                onClick={() =>
                                    void showHistory(historyPage + 1)
                                }
                            >
                                Next page
                            </button>
                        </div>
                        {historyError && (
                            <p role="alert">
                                {historyError}{" "}
                                <button
                                    onClick={() =>
                                        void showHistory(historyPage)
                                    }
                                >
                                    Retry history
                                </button>
                            </p>
                        )}
                        {historyLoading ? (
                            <p role="status">Loading rounds...</p>
                        ) : history.length === 0 ? (
                            <p>No rounds yet. Summon your first storm.</p>
                        ) : (
                            history.map((h) => (
                                <div
                                    className={styles.historyRow}
                                    key={h.roundId}
                                >
                                    <span>
                                        {h.mode === "BonusBuy"
                                            ? "BONUS BUY"
                                            : h.mode === "FreeSpin"
                                              ? "FREE SPIN"
                                              : "BASE ROUND"}
                                        <small>
                                            {h.gameId} /{" "}
                                            {new Date(
                                                h.createdAt,
                                            ).toLocaleString()}
                                        </small>
                                        <small>
                                            {h.roundId.slice(0, 8)} · v
                                            {h.mathVersion}
                                        </small>
                                    </span>
                                    <span>{credits(h.stake)}</span>
                                    <strong>+{credits(h.award)}</strong>
                                    <button
                                        aria-label={
                                            "Replay " + h.roundId.slice(0, 8)
                                        }
                                        disabled={
                                            busy ||
                                            (bonus && h.gameId !== game?.gameId)
                                        }
                                        title={
                                            bonus && h.gameId !== game?.gameId
                                                ? "Finish the active bonus before switching games"
                                                : "Replay without changing your balance"
                                        }
                                        onClick={() => void replay(h.roundId)}
                                    >
                                        REPLAY
                                    </button>
                                </div>
                            ))
                        )}
                    </Dialog>
                )}
            </main>
            <div className={styles.siteFooter}>
                <strong>Built for the storm.</strong>
                <span>
                    Portfolio arcade / Virtual credits only / No cash value
                </span>
            </div>
        </div>
    );
}
function Icon({ name }: { name: string }) {
    const paths: Record<string, string> = {
        expand: "M8 3H3V8M16 3H21V8M3 16V21H8M21 16V21H16",
        bolt: "M15 2 5 14H12L9 23 21 10H14Z",
        sound: "M3 9H7L12 5V19L7 15H3ZM16 8Q21 12 16 16M19 5Q26 12 19 19",
        mute: "M3 9H7L12 5V19L7 15H3ZM17 9L22 15M22 9L17 15",
        settings: "M4 6H20M4 12H20M4 18H20M8 3V9M16 9V15M10 15V21",
        info: "M12 10V17M12 7V7.2M22 12A10 10 0 1 1 2 12A10 10 0 1 1 22 12",
        history: "M3 11A9 9 0 1 1 5 18M3 4V11H10M12 7V12L16 15",
        spin: "M19 8A8 8 0 1 0 20 15M19 3V9H13",
        replay: "M19 8A8 8 0 1 0 20 15M19 3V9H13",
        skip: "M5 5 14 12 5 19ZM17 5V19",
    };
    return (
        <svg
            viewBox="0 0 26 24"
            width="22"
            height="22"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.6"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
        >
            <path d={paths[name] ?? paths.info} />
        </svg>
    );
}
