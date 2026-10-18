import { Dialog } from "../../components/Dialog";
import styles from "../../app/App.module.css";
import type { Rules, Session } from "../../core/types";
import { credits } from "../../core/types";
interface BonusBuyDialogProps {
rules: Rules | undefined;
buyCost: number;
buyMultiple: number;
busy: boolean;
bonus: boolean;
hasPending: boolean;
ready: boolean;
session: Session | undefined;
onPurchase: () => void;
onClose: () => void;
}
export function BonusBuyDialog({rules,buyCost,buyMultiple,busy,bonus,hasPending,ready,session,onPurchase,onClose}: BonusBuyDialogProps) { return (<Dialog
                        title="ThunderGate bonus"
                        onClose={() => onClose()}
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
                                    onClose();
                                    onPurchase();
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
                    </Dialog>); }
