import {
    forwardRef,
    useEffect,
    useImperativeHandle,
    useRef,
    useState,
} from "react";
import type { StormScene } from "../renderer/StormScene";
import type { Game, Round, GamePhase } from "../core/types";
import type { AudioManager } from "../audio/AudioManager";
export interface SceneHandle {
    begin: (turbo?: boolean) => void;
    play: (
        r: Round,
        turbo: boolean,
        onStep: (step: number, r: Round) => void,
        onPhase?: (phase: GamePhase) => void,
    ) => Promise<void>;
    skip: () => void;
    cancel: () => void;
    restore: (r?: Round) => void;
}
export const GameCanvas = forwardRef<
    SceneHandle,
    { game: Game; audio: AudioManager; onReady: () => void }
>(({ game, audio, onReady }, ref) => {
    const host = useRef<HTMLDivElement>(null),
        scene = useRef<StormScene | null>(null),
        ready = useRef(onReady);
    const [error, setError] = useState("");
    ready.current = onReady;
    useImperativeHandle(
        ref,
        () => ({
            begin: (turbo) => scene.current?.begin(turbo),
            play: async (r, t, s, p) => {
                await scene.current?.play(r, t, s, p);
            },
            skip: () => scene.current?.skip(),
            cancel: () => scene.current?.cancel(),
            restore: (r) => scene.current?.restore(r),
        }),
        [],
    );
    useEffect(() => {
        let live = true;
        void (async () => {
            const { StormScene } = await import("../renderer/StormScene");
            if (!live) return;
            const renderer = new StormScene(game, audio);
            scene.current = renderer;
            await renderer.mount(host.current!);
            if (live) ready.current();
        })().catch(() => {
            if (live)
                setError(
                    "The renderer could not start. Enable WebGL and reload.",
                );
        });
        return () => {
            live = false;
            scene.current?.dispose();
            scene.current = null;
        };
    }, [game, audio]);
    return (
        <div className="canvas-host" ref={host}>
            {error && <p role="alert">{error}</p>}
        </div>
    );
});
