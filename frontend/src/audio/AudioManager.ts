export type Sound =
    | "spin_start" | "spin_loop" | "reel_stop" | "tumble_win"
    | "symbol_drop" | "multiplier_collect" | "thunder_strike"
    | "scatter_appear" | "bonus_start" | "bonus_retrigger"
    | "bonus_end" | "big_win" | "button_click" | "ambient_music"
    // Kept as aliases while existing UI callers migrate to the event names.
    | "spin" | "drop" | "tumble" | "thunder" | "multiplier"
    | "scatter" | "retrigger" | "bonus_complete" | "click";

export interface Settings {
    muted: boolean;
    music: number;
    sfx: number;
}

const aliases: Partial<Record<Sound, Sound>> = {
    spin: "spin_start", drop: "symbol_drop", tumble: "tumble_win",
    thunder: "thunder_strike", multiplier: "multiplier_collect",
    scatter: "scatter_appear", retrigger: "bonus_retrigger",
    bonus_complete: "bonus_end", click: "button_click",
};

const notes: Record<Sound, number[]> = {
    spin_start: [140, 210], spin_loop: [72], reel_stop: [330, 440],
    tumble_win: [440, 554, 659], symbol_drop: [330],
    multiplier_collect: [659, 880, 1318], thunder_strike: [55, 82],
    scatter_appear: [784, 1046], bonus_start: [330, 440, 659, 880],
    bonus_retrigger: [659, 880, 1046], bonus_end: [880, 659, 440],
    big_win: [440, 554, 659, 880, 1108], button_click: [400],
    ambient_music: [82], spin: [140, 210], drop: [330], tumble: [440, 554, 659],
    thunder: [55, 82], multiplier: [659, 880, 1318], scatter: [784, 1046],
    retrigger: [659, 880, 1046], bonus_complete: [880, 659, 440], click: [400],
};

type Voice = { oscillator: OscillatorNode; gain: GainNode };

export class AudioManager {
    settings: Settings;
    private context?: AudioContext;
    private sfxBus?: GainNode;
    private musicBus?: GainNode;
    private voices = new Set<Voice>();
    private loops = new Map<Sound, Voice>();
    private times = new Map<Sound, number>();
    private disposed = false;

    constructor() {
        try {
            this.settings = {
                muted: true, music: 0.12, sfx: 0.4,
                ...JSON.parse(localStorage.getItem("tf-audio") ?? "{}"),
            };
            this.settings.music = this.clamp(this.settings.music);
            this.settings.sfx = this.clamp(this.settings.sfx);
            this.settings.muted = Boolean(this.settings.muted);
        } catch {
            this.settings = { muted: true, music: 0.12, sfx: 0.4 };
        }
    }

    private clamp(value: number) {
        return Number.isFinite(value) ? Math.max(0, Math.min(1, value)) : 0;
    }

    initialize() {
        if (this.context) return;
        this.disposed = false;
        try {
            const context = new AudioContext();
            const sfx = context.createGain();
            const music = context.createGain();
            sfx.connect(context.destination);
            music.connect(context.destination);
            this.context = context;
            this.sfxBus = sfx;
            this.musicBus = music;
            this.applyVolumes();
            this.startLoop("ambient_music");
        } catch (error) {
            console.warn("Thunderfall audio is unavailable", error);
        }
    }

    async unlock() {
        this.initialize();
        try {
            await this.context?.resume();
        } catch (error) {
            console.warn("Thunderfall audio could not be resumed", error);
        }
    }

    private applyVolumes() {
        const context = this.context;
        if (!context) return;
        const now = context.currentTime;
        this.sfxBus?.gain.setTargetAtTime(this.settings.muted ? 0 : this.settings.sfx, now, 0.02);
        this.musicBus?.gain.setTargetAtTime(this.settings.muted ? 0 : this.settings.music, now, 0.08);
    }

    configure(patch: Partial<Settings>) {
        this.settings = {
            muted: patch.muted ?? this.settings.muted,
            music: patch.music === undefined ? this.settings.music : this.clamp(patch.music),
            sfx: patch.sfx === undefined ? this.settings.sfx : this.clamp(patch.sfx),
        };
        try { localStorage.setItem("tf-audio", JSON.stringify(this.settings)); }
        catch { console.warn("Thunderfall audio settings could not be saved"); }
        this.applyVolumes();
    }

    setMusicVolume(value: number) { this.configure({ music: value }); }
    setSfxVolume(value: number) { this.configure({ sfx: value }); }
    setMuted(value: boolean) { this.configure({ muted: value }); }

    play(event: Sound) {
        const sound = aliases[event] ?? event;
        if (sound === "spin_loop" || sound === "ambient_music") {
            this.startLoop(sound);
            return;
        }
        const context = this.context;
        if (!context || context.state !== "running" || this.settings.muted) return;
        const now = context.currentTime;
        if (now - (this.times.get(sound) ?? -10) < 0.045) return;
        this.times.set(sound, now);
        const duration = sound === "thunder_strike" ? 0.42 : 0.24;
        for (const [index, frequency] of notes[sound].entries()) {
            if (this.voices.size >= 16) break;
            const oscillator = context.createOscillator();
            const gain = context.createGain();
            oscillator.type = sound === "thunder_strike" ? "sawtooth" : "sine";
            oscillator.frequency.value = frequency;
            const start = now + index * 0.055;
            gain.gain.setValueAtTime(0, start);
            gain.gain.linearRampToValueAtTime(0.8, start + 0.012);
            gain.gain.exponentialRampToValueAtTime(0.001, start + duration);
            oscillator.connect(gain).connect(this.sfxBus!);
            const voice = { oscillator, gain };
            this.voices.add(voice);
            oscillator.onended = () => {
                this.voices.delete(voice);
                oscillator.disconnect();
                gain.disconnect();
            };
            oscillator.start(start);
            oscillator.stop(start + duration + index * 0.055);
        }
    }

    startLoop(event: Sound) {
        const sound = aliases[event] ?? event;
        if (this.loops.has(sound) || this.disposed) return;
        if (!this.context) {
            if (sound === "ambient_music") this.initialize();
            if (!this.context) return;
        }
        const context = this.context;
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.type = sound === "spin_loop" ? "triangle" : "sine";
        oscillator.frequency.value = sound === "spin_loop" ? 72 : 82.4069;
        gain.gain.value = sound === "ambient_music" ? 0.25 : 0.09;
        oscillator.connect(gain).connect(sound === "ambient_music" ? this.musicBus! : this.sfxBus!);
        oscillator.start();
        this.loops.set(sound, { oscillator, gain });
    }

    stopLoop(event: Sound) {
        const sound = aliases[event] ?? event;
        const voice = this.loops.get(sound);
        if (!voice) return;
        this.loops.delete(sound);
        try {
            const context = this.context!;
            voice.gain.gain.setTargetAtTime(0, context.currentTime, 0.025);
            voice.oscillator.stop(context.currentTime + 0.1);
            voice.oscillator.onended = () => { voice.oscillator.disconnect(); voice.gain.disconnect(); };
        } catch { /* Context may have closed while the scene was unmounting. */ }
    }

    stopAll() {
        for (const event of [...this.loops.keys()]) this.stopLoop(event);
        for (const voice of this.voices) {
            try { voice.oscillator.stop(); } catch { /* Already ended. */ }
        }
        this.voices.clear();
    }

    dispose() {
        if (this.disposed) return;
        this.disposed = true;
        this.stopAll();
        void this.context?.close();
        this.context = undefined;
    }
}
