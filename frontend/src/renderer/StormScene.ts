import {
    Application,
    Assets,
    Container,
    Graphics,
    Sprite,
    Text,
    Texture,
} from "pixi.js";
import { gsap } from "gsap";
import type { Board, Cell, Game, Round, Step, GamePhase } from "../core/types";
import type { AudioManager } from "../audio/AudioManager";
const W = 112,
    H = 88,
    P = 12;
export class StormScene {
    private app = new Application();
    private entries = new Map<number, Container>();
    private cellSprites = new Map<number, Sprite>();
    private multiplierLabels = new Map<number, Text>();
    private cellPositions = new Map<number, number>();
    private textures = new Map<string, Texture>();
    private overlay = new Graphics();
    private symbols = new Container();
    private particles: Graphics[] = [];
    private tweens = new Map<gsap.core.Tween, () => void>();
    private clearing: Promise<void> = Promise.resolve();
    private sequence = 0;
    private disposed = false;
    private initialized = false;
    private destroyed = false;
    private skipping = false;
    private current?: Round;
    private last?: Round;
    private time = 0;
    constructor(
        private game: Game,
        private audio: AudioManager,
    ) {}
    async mount(host: HTMLDivElement) {
        const width = this.game.layout.columns * W + 2 * P,
            height = this.game.layout.rows * H + 2 * P;
        await this.app.init({
            width,
            height,
            backgroundAlpha: 0,
            antialias: true,
            resolution: Math.min(devicePixelRatio, 2),
            autoDensity: true,
            preference: "webgl",
        });
        this.initialized = true;
        if (this.disposed) {
            this.destroy();
            return;
        }
        const textures = await Promise.all(
            this.game.symbols.map(
                async (s) =>
                    [
                        s,
                        await Assets.load<Texture>(
                            "/assets/symbols/" + s + ".svg",
                        ),
                    ] as const,
            ),
        );
        if (this.disposed) {
            this.destroy();
            return;
        }
        textures.forEach(([s, t]) => this.textures.set(s, t));
        host.appendChild(this.app.canvas);
        this.app.canvas.setAttribute("role", "img");
        this.app.canvas.setAttribute(
            "aria-label",
            `${this.game.layout.columns} by ${this.game.layout.rows} slot board`,
        );
        this.app.stage.addChild(
            new Graphics().roundRect(0, 0, width, height, 4).fill(0x260c36),
        );
        for (let c = 0; c < this.game.layout.columns; c++)
            for (let r = 0; r < this.game.layout.rows; r++)
                this.app.stage.addChild(
                    new Graphics()
                        .roundRect(
                            P + c * W + 3,
                            P + r * H + 3,
                            W - 6,
                            H - 6,
                            3,
                        )
                        .fill({
                            color: c % 2 ? 0x431546 : 0x36113e,
                            alpha: 0.72,
                        })
                        .stroke({ color: 0xf2c86c, alpha: 0.09, width: 1 }),
                );
        const mask = new Graphics().rect(P, P, width - 2 * P, height - 2 * P).fill(0xffffff);
        this.app.stage.addChild(mask, this.symbols);
        this.symbols.mask = mask;
        this.app.stage.addChild(this.overlay);
        // Intro is a display-only arrangement, never a claimed round outcome.
        const intro: Board = {
            layout: this.game.layout,
            cells: Array.from({ length: this.game.layout.size }, (_, i) => ({
                symbol: this.game.symbols[
                    (i * 3 + (i % 5)) % this.game.symbols.length
                ],
                identity: i + 1,
                multiplier: 0,
                collected: false,
            })),
        };
        this.show(intro);
        for (let i = 0; i < 24; i++) {
            const dot = new Graphics()
                .circle(0, 0, 0.6 + (i % 3) * 0.4)
                .fill(0xffeab1);
            dot.position.set((i * 97) % width, (i * 43) % height);
            dot.alpha = 0.15;
            this.particles.push(dot);
            this.app.stage.addChild(dot);
        }
        this.app.ticker.add((t) => {
            if (this.disposed) return;
            this.time += t.deltaMS;
            if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
            this.particles.forEach((p, i) => {
                p.y -= t.deltaMS * (0.006 + i * 0.0002);
                if (p.y < 0) p.y = height;
                p.alpha = 0.1 + 0.12 * Math.sin(this.time / 1800 + i);
            });
        });
    }
    private position(index: number) {
        return {
            x: P + (Math.floor(index / this.game.layout.rows) + 0.5) * W,
            y: P + ((index % this.game.layout.rows) + 0.5) * H,
        };
    }
    private cell(cell: Cell, index: number) {
        const c = new Container(),
            sprite = new Sprite(this.textures.get(cell.symbol)!);
        sprite.anchor.set(0.5);
        sprite.width = 82;
        sprite.height = 82;
        c.addChild(sprite);
        this.cellSprites.set(cell.identity, sprite);
        const pos = this.position(index);
        c.position.set(pos.x, pos.y);
        if (cell.symbol === "StormOrb" && cell.multiplier > 0) {
            const text = new Text({
                text: "×" + cell.multiplier,
                style: {
                    fontFamily: "Anton, Impact, sans-serif",
                    fontSize: 22,
                    fontWeight: "bold",
                    fill: 0xfff0a0,
                    stroke: { color: 0x30103c, width: 4 },
                },
            });
            text.anchor.set(0.5);
            text.y = 26;
            c.addChild(text);
            this.multiplierLabels.set(cell.identity, text);
        }
        this.symbols.addChild(c);
        this.entries.set(cell.identity, c);
        this.cellPositions.set(cell.identity, index);
        return c;
    }
    private destroyCell(identity: number) {
        const c = this.entries.get(identity);
        if (!c) return;
        this.symbols.removeChild(c);
        c.destroy({ children: true });
        this.entries.delete(identity);
        this.cellPositions.delete(identity);
        this.cellSprites.delete(identity);
        this.multiplierLabels.delete(identity);
    }
    show(board: Board) {
        const present = new Set(board.cells.map((cell) => cell.identity));
        for (const [identity] of this.entries) {
            if (present.has(identity)) continue;
            this.destroyCell(identity);
        }
        board.cells.forEach((cell, i) => {
            const c = this.entries.get(cell.identity);
            if (!c) {
                this.cell(cell, i);
                return;
            }
            const sprite = this.cellSprites.get(cell.identity)!;
            sprite.texture = this.textures.get(cell.symbol)!;
            const label = this.multiplierLabels.get(cell.identity);
            if (cell.symbol === "StormOrb" && cell.multiplier > 0) {
                if (label) label.text = "×" + cell.multiplier;
                else {
                    const text = new Text({ text: "×" + cell.multiplier, style: {
                        fontFamily: "Anton, Impact, sans-serif", fontSize: 22,
                        fontWeight: "bold", fill: 0xfff0a0,
                        stroke: { color: 0x30103c, width: 4 },
                    } });
                    text.anchor.set(0.5);
                    text.y = 26;
                    c.addChild(text);
                    this.multiplierLabels.set(cell.identity, text);
                }
            } else if (label) {
                c.removeChild(label);
                label.destroy();
                this.multiplierLabels.delete(cell.identity);
            }
            const pos = this.position(i);
            c.position.set(pos.x, pos.y);
            c.alpha = 1;
            c.visible = true;
            c.scale.set(1);
            this.cellPositions.set(cell.identity, i);
        });
        this.app.stage.setChildIndex(
            this.overlay,
            this.app.stage.children.length - 1,
        );
    }
    begin(turbo = false) {
        this.skipping = false;
        this.current = undefined;
        this.overlay.clear();
        const sequence = ++this.sequence;
        const reduced = matchMedia("(prefers-reduced-motion: reduce)").matches;
        const speed = turbo ? 0.38 : 1;
        const oldCells = [...this.entries];
        this.audio.play("spin_start");
        // Finish clearing every reel before any result symbols are created.
        this.clearing = Promise.all(oldCells.map(async ([identity, c]) => {
            if (!reduced) {
                const index = this.cellPositions.get(identity) ?? 0;
                await this.animate(c, {
                    y: c.y + (this.game.layout.rows + 1) * H,
                    delay: Math.floor(index / this.game.layout.rows) * 0.025 * speed,
                    ease: "power2.in",
                }, 0.34 * speed);
            }
            if (sequence === this.sequence && !this.disposed && !this.skipping)
                this.destroyCell(identity);
        })).then(() => {});
    }
    private animate(target: object, vars: gsap.TweenVars, duration: number) {
        if (this.disposed || this.skipping) return Promise.resolve();
        return new Promise<void>((resolve) => {
            const done = () => {
                this.tweens.delete(tween);
                resolve();
            };
            const tween = gsap.to(target, {
                ...vars,
                duration,
                onComplete: done,
                onInterrupt: done,
            });
            this.tweens.set(tween, resolve);
        });
    }
    async play(
        round: Round,
        turbo: boolean,
        onStep: (step: number, round: Round) => void,
        onPhase: (phase: GamePhase) => void = () => {},
    ) {
        this.current = round;
        const reduced = matchMedia("(prefers-reduced-motion: reduce)").matches;
        const speed = reduced ? 0 : turbo ? 0.38 : 1;
        if (this.skipping || reduced) {
            this.skip();
            this.show(round.outcome.finalBoard);
            this.last = round;
            return;
        }
        await this.clearing;
        if (this.disposed || this.skipping) return;
        await this.animate({}, {}, 0.08 * speed);
        if (this.disposed || this.skipping) return;
        onPhase("Revealing");
        // Round identities may be reused: discard the old board before revealing columns.
        for (const identity of [...this.entries.keys()]) this.destroyCell(identity);
        await Promise.all(Array.from({ length: this.game.layout.columns }, async (_, column) => {
            if (column) await this.animate({}, {}, column * 0.095 * speed);
            if (this.disposed || this.skipping) return;
            await this.dropReel(column, round.initialBoard, 0.64 * speed);
            if (!this.disposed && !this.skipping) this.audio.play("reel_stop");
        }));
        if (this.disposed || this.skipping) return;
        await this.animate({}, {}, 0.2 * speed);
        if (round.initialBoard.cells.some((c) => c.symbol === "ThunderGate"))
            this.audio.play("scatter");
        for (const step of round.cascadeSteps) {
            if (this.disposed || this.skipping) break;
            onPhase("Cascading");
            onStep(step.stepIndex, round);
            await this.tumble(step, speed, onPhase);
        }
        if (!this.disposed) {
            this.show(round.outcome.finalBoard);
            this.overlay.clear();
            this.last = round;
            for (const hint of round.presentationHints) {
                if (hint.type === "BonusIntro") this.audio.play("bonus_start");
                if (hint.type === "Retrigger") this.audio.play("retrigger");
                if (hint.type === "BonusComplete")
                    this.audio.play("bonus_complete");
                if (hint.type === "BigWin" || hint.type === "MegaWin")
                    this.audio.play("big_win");
            }
        }
    }
    private async tumble(step: Step, speed: number, onPhase: (phase: GamePhase) => void) {
        this.overlay.clear();
        const winning: Container[] = [];
        for (const p of step.removedPositions) {
            const pos = this.position(p);
            this.overlay
                .roundRect(
                    pos.x - W / 2 + 5,
                    pos.y - H / 2 + 5,
                    W - 10,
                    H - 10,
                    8,
                )
                .fill({ color: 0xdac58c, alpha: 0.1 })
                .stroke({ color: 0xe9d698, alpha: 0.8, width: 1.5 });
            const c = this.entries.get(step.boardBefore.cells[p].identity);
            if (c) winning.push(c);
        }
        this.audio.play("tumble_win");
        await Promise.all(
            winning.map((c) =>
                this.animate(c.scale, { x: 1.1, y: 1.1 }, 0.24 * speed),
            ),
        );
        await this.animate({}, {}, 0.28 * speed);
        if (this.disposed || this.skipping) return;
        if (step.multiplierEvents.length) onPhase("ApplyingMultipliers");
        for (const orb of step.multiplierEvents) {
            if (this.disposed || this.skipping) return;
            const pos = this.position(orb.position);
            this.overlay
                .moveTo(pos.x - 15, 0)
                .lineTo(pos.x + 8, pos.y * 0.38)
                .lineTo(pos.x - 5, pos.y * 0.36)
                .lineTo(pos.x, pos.y)
                .stroke({ color: 0xc8fcff, width: 3, alpha: 0.9 });
            this.audio.play("thunder_strike");
            const c = this.entries.get(orb.identity);
            if (c)
                await this.animate(
                    c.scale,
                    { x: 1.2, y: 1.2, yoyo: true, repeat: 1 },
                    0.12 * speed,
                );
            this.audio.play("multiplier_collect");
        }
        if (step.multiplierEvents.length) onPhase("Cascading");
        await Promise.all(
            winning.map((c) => this.animate(c, { alpha: 0 }, 0.22 * speed)),
        );
        if (this.disposed || this.skipping) return;
        this.overlay.clear();
        for (const p of step.removedPositions) {
            this.destroyCell(step.boardBefore.cells[p].identity);
        }
        await Promise.all(
            step.gravityMoves.map((move) => {
                const c = this.entries.get(move.cell.identity);
                return c
                    ? this.animate(
                          c,
                          { ...this.position(move.to), ease: "power2.in" },
                          0.38 * speed,
                      )
                    : Promise.resolve();
            }),
        );
        if (this.disposed || this.skipping) return;
        this.audio.play("symbol_drop");
        await Promise.all(
            step.newSymbols.map((spawn) => {
                const c = this.cell(spawn.cell, spawn.position),
                    pos = this.position(spawn.position);
                c.y = pos.y - this.game.layout.rows * H;
                c.alpha = 1;
                return this.animate(
                    c,
                    { y: pos.y, alpha: 1, ease: "power2.out" },
                    0.52 * speed,
                );
            }),
        );
        if (!this.disposed && !this.skipping) this.show(step.boardAfter);
        await this.animate({}, {}, 0.18 * speed);
    }
    private async dropReel(column: number, board: Board, duration: number) {
        const rows = this.game.layout.rows;
        await Promise.all(board.cells.slice(column * rows, (column + 1) * rows).map((cell, row) => {
            const position = column * rows + row;
            const c = this.cell(cell, position);
            const pos = this.position(position);
            c.y = pos.y - rows * H;
            return this.animate(c, { y: pos.y, ease: "sine.inOut" }, duration);
        }));
    }
    skip() {
        this.skipping = true;
        this.sequence++;
        this.overlay.clear();
        for (const [tween, resolve] of this.tweens) {
            tween.kill();
            resolve();
        }
        this.tweens.clear();
        if (this.current && !this.disposed)
            this.show(this.current.outcome.finalBoard);
    }
    cancel() { this.skip(); }
    restore(round?: Round) {
        this.skip();
        this.skipping = false;
        this.current = round ?? this.last;
        if (this.current) {
            this.show(this.current.outcome.finalBoard);
            this.last = this.current;
        }
    }
    private destroy() {
        if (this.initialized && !this.destroyed) {
            this.destroyed = true;
            this.app.destroy(true, { children: true, texture: false });
        }
    }
    dispose() {
        this.disposed = true;
        this.skip();
        this.destroy();
    }
}
