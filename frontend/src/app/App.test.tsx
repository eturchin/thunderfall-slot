import {
    render,
    screen,
    fireEvent,
    waitFor,
    act,
} from "@testing-library/react";
import { beforeEach, describe, it, expect, vi } from "vitest";
import { api, ApiError } from "../api/client";
import App from "./App";
import type {
    Game,
    Session,
    Round,
    Outcome,
    Rules,
    Group,
    Orb,
} from "../core/types";
import trace from "../../../tests/fixtures/engine-bonus-cycle.json";
vi.mock("../api/client", async () => {
    const original =
        await vi.importActual<typeof import("../api/client")>("../api/client");
    return {
        ...original,
        api: {
            games: vi.fn(),
            current: vi.fn(),
            create: vi.fn(),
            round: vi.fn(),
            history: vi.fn(),
            historyPage: vi.fn(),
            switchGame: vi.fn(),
            replay: vi.fn(),
            reset: vi.fn(),
            rules: vi.fn(),
        },
    };
});
vi.mock("../components/GameCanvas", async () => {
    const React = await import("react");
    return {
        GameCanvas: React.forwardRef(
            ({ onReady }: { onReady: () => void }, ref) => {
                React.useImperativeHandle(ref, () => ({
                    begin: vi.fn(),
                    play: async () => {},
                    skip: vi.fn(),
                    restore: vi.fn(),
                }));
                React.useEffect(() => {
                    onReady();
                }, []);
                return <div data-testid="game-canvas" />;
            },
        ),
    };
});
vi.mock("../audio/AudioManager", () => ({
    AudioManager: class {
        settings = { muted: true, music: 0.12, sfx: 0.4 };
        unlock = async () => {};
        play = vi.fn();
        dispose = vi.fn();
        configure(p: object) {
            this.settings = { ...this.settings, ...p };
        }
    },
}));
const game: Game = {
    gameId: "thunderfall",
    mathVersion: "1.0.0",
    rulesetHash: trace.rulesetHash,
    name: "Thunderfall: Wrath of the Titans",
    kind: "CountAnywhere",
    layout: { columns: 6, rows: 5, size: 30 },
    stakes: [1000, 2000, 5000, 10000, 20000],
    symbols: ["Crown"],
};
const session: Session = {
    id: "session",
    gameId: "thunderfall",
    mathVersion: "1.0.0",
    balance: 1000000,
    featureState: { bonus: null, isBonus: false },
    version: 0,
};
const outcome = trace.rounds[0] as unknown as Outcome & {
    rawWin: number;
    winningGroups: Group[];
    multiplierEvents: Orb[];
};
const round: Round = {
    roundId: outcome.roundId,
    gameId: "thunderfall",
    mathVersion: "1.0.0",
    mode: "Base",
    stake: 2000,
    initialBoard: outcome.initialBoard,
    cascadeSteps: outcome.cascadeSteps,
    winningGroups: outcome.winningGroups,
    multiplierEvents: outcome.multiplierEvents,
    rawWin: outcome.rawWin,
    finalWin: outcome.finalWin,
    balanceAfter: 998000 + outcome.finalWin,
    featureState: outcome.finalMathState,
    presentationHints: [],
    outcome,
};
const rules: Rules = {
    bonusBuy: {
        policyVersion: "storm-buy-1.0.0",
        priceNumerator: 455,
        priceDenominator: 10,
        initialSpins: 15,
    },
    paytable: {},
    maxWinMultiplier: 5000,
    creditScale: 100,
    payScale: 1000000,
    featureRules: { initialSpins: 15, retriggerSpins: 5 },
};
async function ready() {
    render(<App />);
    const b = await screen.findByRole("button", { name: /^Spin$/ });
    await waitFor(() => expect(b).toBeEnabled());
    return b;
}
beforeEach(() => {
    vi.mocked(api.games).mockResolvedValue([game]);
    vi.mocked(api.current).mockResolvedValue(session);
    vi.mocked(api.create).mockResolvedValue(session);
    vi.mocked(api.rules).mockResolvedValue(rules);
    vi.mocked(api.history).mockResolvedValue([]);
    vi.mocked(api.round).mockResolvedValue(round);
    vi.mocked(api.replay).mockResolvedValue(round);
});
describe("Authoritative client", () => {
    it("shows server balance and sends integer minor-unit stake", async () => {
        fireEvent.click(await ready());
        await waitFor(() => expect(api.round).toHaveBeenCalled());
        expect(vi.mocked(api.round).mock.calls[0][0].stake).toBe(2000);
        expect(screen.getByTestId("balance")).toHaveTextContent("10,000.00");
    });
    it("prevents a second request while skip is available", async () => {
        let resolve!: (r: Round) => void;
        vi.mocked(api.round).mockReturnValue(new Promise((r) => (resolve = r)));
        fireEvent.click(await ready());
        fireEvent.click(screen.getByRole("button", { name: "Skip animation" }));
        expect(api.round).toHaveBeenCalledTimes(1);
        await act(async () => resolve(round));
    });
    it("paginates all stored rounds and keeps navigation inside the game", async () => {
        vi.mocked(api.historyPage)
            .mockResolvedValueOnce({
                items: [],
                total: 43,
                page: 1,
                pageSize: 20,
            })
            .mockResolvedValueOnce({
                items: [],
                total: 43,
                page: 2,
                pageSize: 20,
            });
        await ready();
        const history = screen.getByRole("button", { name: "Round history" });
        expect(history.closest("main")).toHaveAttribute(
            "aria-label",
            "Game window",
        );
        fireEvent.click(history);
        expect(await screen.findByText("Page 1 of 3")).toBeVisible();
        fireEvent.click(screen.getByRole("button", { name: "Next page" }));
        expect(await screen.findByText("Page 2 of 3")).toBeVisible();
        expect(api.historyPage).toHaveBeenLastCalledWith(2);
    });
    it("confirms the exact bonus purchase price and sends an idempotent purchase command", async () => {
        await ready();
        fireEvent.click(screen.getByRole("button", { name: /BUY BONUS/ }));
        expect(screen.getByRole("dialog")).toHaveTextContent("910.00");
        fireEvent.click(
            screen.getByRole("button", { name: "Buy & play first free spin" }),
        );
        await waitFor(() =>
            expect(api.round).toHaveBeenCalledWith(
                expect.objectContaining({
                    buyBonus: true,
                    stake: 2000,
                    idempotencyKey: expect.any(String),
                }),
            ),
        );
    });
    it("adjusts stake and persists turbo", async () => {
        await ready();
        fireEvent.click(screen.getByRole("button", { name: "Increase stake" }));
        expect(screen.getByTestId("stake")).toHaveTextContent("50.00");
        fireEvent.click(screen.getByRole("button", { name: /TURBO/ }));
        expect(localStorage.getItem("tf-turbo")).toBe("true");
    });
    it("has mute, separate volume and rules dialogs", async () => {
        await ready();
        fireEvent.click(screen.getByRole("button", { name: "Unmute sound" }));
        expect(
            screen.getByRole("button", { name: "Mute sound" }),
        ).toBeInTheDocument();
        fireEvent.click(screen.getByRole("button", { name: "Sound settings" }));
        expect(
            screen.getByRole("slider", { name: "Music volume" }),
        ).toBeVisible();
        fireEvent.click(screen.getByRole("button", { name: "Close dialog" }));
        fireEvent.click(screen.getByRole("button", { name: "Game rules" }));
        expect(screen.getByRole("dialog")).toHaveTextContent("5,000");
    });
    it("recovers session from the cookie endpoint without creating another", async () => {
        await ready();
        expect(api.current).toHaveBeenCalled();
        expect(api.create).not.toHaveBeenCalled();
    });
    it("locks trigger stake and resumes a persisted bonus", async () => {
        vi.mocked(api.current).mockResolvedValue({
            ...session,
            featureState: outcome.finalMathState,
        });
        render(<App />);
        const b = await screen.findByRole("button", { name: "Free spin" });
        await waitFor(() => expect(b).toBeEnabled());
        expect(
            screen.getByRole("button", { name: "Increase stake" }),
        ).toBeDisabled();
        fireEvent.click(b);
        await waitFor(() => expect(api.round).toHaveBeenCalled());
        expect(vi.mocked(api.round).mock.calls[0][0].stake).toBeUndefined();
    });
    it("keeps the request key after an ambiguous network failure", async () => {
        vi.mocked(api.round).mockRejectedValueOnce(new TypeError("network"));
        fireEvent.click(await ready());
        await screen.findByRole("alert");
        const key = vi.mocked(api.round).mock.calls[0][0].idempotencyKey;
        fireEvent.click(screen.getByRole("button", { name: "Retry round" }));
        await waitFor(() => expect(api.round).toHaveBeenCalledTimes(2));
        expect(vi.mocked(api.round).mock.calls[1][0].idempotencyKey).toBe(key);
    });
    it.each([400, 403, 404, 409, 422])(
        "keeps the accepted round key when session refresh fails with %s",
        async (status) => {
            const button = await ready();
            vi.mocked(api.current).mockRejectedValueOnce(
                new ApiError("RefreshFailed", status),
            );
            fireEvent.click(button);
            await screen.findByRole("alert");
            const command = vi.mocked(api.round).mock.calls[0][0];
            expect(
                JSON.parse(localStorage.getItem("tf-pending")!),
            ).toMatchObject({ idempotencyKey: command.idempotencyKey });
            fireEvent.click(
                screen.getByRole("button", { name: "Retry round" }),
            );
            await waitFor(() => expect(api.round).toHaveBeenCalledTimes(2));
            expect(vi.mocked(api.round).mock.calls[1][0]).toEqual(command);
        },
    );
    it("releases controls and sends no round until its key can be persisted", async () => {
        const button = await ready();
        const storage = vi
            .spyOn(Storage.prototype, "setItem")
            .mockImplementation(() => {
                throw new DOMException("Storage full", "QuotaExceededError");
            });
        try {
            fireEvent.click(button);
            expect(await screen.findByRole("alert")).toHaveTextContent(
                "Could not save the round request",
            );
            expect(api.round).not.toHaveBeenCalled();
            const retry = screen.getByRole("button", { name: "Retry round" });
            expect(retry).toBeEnabled();
            fireEvent.click(retry);
            await waitFor(() => expect(storage).toHaveBeenCalledTimes(2));
            expect(api.round).not.toHaveBeenCalled();
            const saved = JSON.parse(storage.mock.calls[0][1]);
            storage.mockRestore();
            fireEvent.click(
                screen.getByRole("button", { name: "Retry round" }),
            );
            await waitFor(() => expect(api.round).toHaveBeenCalledTimes(1));
            expect(vi.mocked(api.round).mock.calls[0][0].idempotencyKey).toBe(
                saved.idempotencyKey,
            );
        } finally {
            storage.mockRestore();
        }
    });
    it("keeps a confirmed result when pending-key cleanup fails", async () => {
        const button = await ready();
        vi.mocked(api.current).mockResolvedValue({
            ...session,
            balance: 998000,
        });
        const storage = vi
            .spyOn(Storage.prototype, "removeItem")
            .mockImplementation(() => {
                throw new DOMException("Storage unavailable", "SecurityError");
            });
        try {
            fireEvent.click(button);
            await waitFor(() =>
                expect(screen.getByTestId("balance")).toHaveTextContent(
                    "9,980.00",
                ),
            );
            expect(screen.queryByRole("alert")).not.toBeInTheDocument();
            expect(
                screen.getByRole("button", { name: /^Spin$/ }),
            ).toBeEnabled();
        } finally {
            storage.mockRestore();
        }
    });
    it("does not show false awards after an API error", async () => {
        vi.mocked(api.round).mockRejectedValueOnce(
            new ApiError("InsufficientBalance", 409),
        );
        fireEvent.click(await ready());
        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Not enough virtual credits",
        );
        expect(screen.getByTestId("balance")).toHaveTextContent("10,000.00");
    });
    it("replay does not execute a new mathematical round", async () => {
        vi.mocked(api.history).mockResolvedValue([
            {
                roundId: round.roundId,
                gameId: "thunderfall",
                mathVersion: "1.0.0",
                mode: "Base",
                stake: 2000,
                award: round.finalWin,
                createdAt: "2026-10-18T00:00:00Z",
            },
        ]);
        await ready();
        fireEvent.click(screen.getByRole("button", { name: "REPLAY ROUND" }));
        await waitFor(() =>
            expect(screen.getByRole("status")).toHaveTextContent(
                "Replay complete",
            ),
        );
        expect(api.round).not.toHaveBeenCalled();
        expect(screen.getByTestId("balance")).toHaveTextContent("10,000.00");
    });
});
