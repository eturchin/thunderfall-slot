import "@testing-library/jest-dom/vitest";
import { afterEach, vi } from "vitest";
import { cleanup } from "@testing-library/react";
afterEach(() => {
    cleanup();
    localStorage.clear();
    vi.clearAllMocks();
});
Object.defineProperty(window, "matchMedia", {
    value: () => ({
        matches: true,
        addEventListener: () => {},
        removeEventListener: () => {},
    }),
});
HTMLDialogElement.prototype.showModal = function () {
    this.setAttribute("open", "");
};
HTMLDialogElement.prototype.close = function () {
    this.removeAttribute("open");
};
