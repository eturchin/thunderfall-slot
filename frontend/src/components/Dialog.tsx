import { useEffect, useRef } from "react";
import type { ReactNode } from "react";
export function Dialog({
    title,
    children,
    onClose,
}: {
    title: string;
    children: ReactNode;
    onClose: () => void;
}) {
    const ref = useRef<HTMLDialogElement>(null);
    useEffect(() => {
        const el = ref.current!;
        el.showModal();
        return () => el.close();
    }, []);
    return (
        <dialog
            ref={ref}
            onCancel={onClose}
            onClick={(e) => {
                if (e.target === e.currentTarget) onClose();
            }}
            aria-label={title}
        >
            <header>
                <h2>{title}</h2>
                <button aria-label="Close dialog" onClick={onClose}>
                    ×
                </button>
            </header>
            {children}
        </dialog>
    );
}
