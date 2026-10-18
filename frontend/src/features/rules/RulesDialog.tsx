import { Dialog } from "../../components/Dialog";
import styles from "../../app/App.module.css";
import type { Rules } from "../../core/types";
interface RulesDialogProps {
thunder: boolean;
rules: Rules | undefined;
onClose: () => void;
}
export function RulesDialog({thunder,rules,onClose}: RulesDialogProps) { return (<Dialog
                        title="Rules & paytable"
                        onClose={() => onClose()}
                    >
                        <p>
                            {thunder
                                ? "8 or more matching ordinary symbols anywhere on the 6×5 board pay once per symbol. All winning symbols disappear together, survivors fall, and the board refills until no wins remain."
                                : "Five fixed paylines pay the best 3, 4 or 5 matching symbols from the left. Wild substitutes ordinary symbols."}
                        </p>
                        <div className={styles.paytable}>
                            <div>
                                <span>SYMBOL</span>
                                {(thunder
                                    ? ["8–9", "10–11", "12+"]
                                    : ["3", "4", "5"]
                                ).map((x) => (
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
                                                {b.payTicks / 1_000_000}×
                                            </b>
                                        ))}
                                    </div>
                                ),
                            )}
                        </div>
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
                    </Dialog>); }
