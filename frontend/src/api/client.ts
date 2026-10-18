import type { Game, Session, Round, HistoryItem, Rules } from "../core/types";
export class ApiError extends Error {
    constructor(
        public code: string,
        public status: number,
    ) {
        super(code);
    }
}
async function request<T>(url: string, body?: object): Promise<T> {
    const r = await fetch("/api/v1" + url, {
        method: body === undefined ? "GET" : "POST",
        credentials: "same-origin",
        headers:
            body === undefined
                ? {}
                : {
                      "Content-Type": "application/json",
                      "X-Thunderfall-Client": "web",
                  },
        body: body === undefined ? undefined : JSON.stringify(body),
    });
    if (!r.ok) {
        const p = await r.json().catch(() => ({ title: "ConnectionError" }));
        throw new ApiError(p.code ?? p.title ?? "ConnectionError", r.status);
    }
    return r.json() as Promise<T>;
}
export const api = {
    games: () => request<Game[]>("/games"),
    game: (gameId: string, version: string) =>
        request<Game>(
            "/games/" + gameId + "?version=" + encodeURIComponent(version),
        ),
    current: () => request<Session>("/sessions/current"),
    create: (gameId: string) => request<Session>("/sessions", { gameId }),
    round: (command: {
        stake?: number;
        idempotencyKey: string;
        buyBonus?: boolean;
    }) => request<Round>("/rounds", command),
    history: () => request<HistoryItem[]>("/rounds/history?limit=1"),
    historyPage: (page: number, pageSize = 20) =>
        request<{
            items: HistoryItem[];
            total: number;
            page: number;
            pageSize: number;
        }>(`/rounds/history/page?page=${page}&pageSize=${pageSize}`),
    switchGame: (gameId: string) =>
        request<Session>("/sessions/game", { gameId }),
    replay: (id: string) => request<Round>("/rounds/" + id),
    reset: () => request<Session>("/sessions/reset", {}),
    rules: (game: string, version?: string) =>
        request<Rules>(
            "/games/" +
                game +
                "/rules" +
                (version ? "?version=" + encodeURIComponent(version) : ""),
        ),
};
