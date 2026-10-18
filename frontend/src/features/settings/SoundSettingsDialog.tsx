import { Dialog } from "../../components/Dialog";
import styles from "../../app/App.module.css";
import type { Settings } from "../../audio/AudioManager";
interface SoundSettingsDialogProps {
sound: Settings;
updateSound: (settings: Partial<Settings>) => void;
onClose: () => void;
}
export function SoundSettingsDialog({sound,updateSound,onClose}: SoundSettingsDialogProps) { return (<Dialog
                        title="Sound settings"
                        onClose={() => onClose()}
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
                    </Dialog>); }
