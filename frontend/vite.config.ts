import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
export default defineConfig({
    plugins: [react()],
    server: {
        port: 5175,
        strictPort: true,
        proxy: {
            "/api": { target: "http://127.0.0.1:5090", changeOrigin: false },
            "/health": "http://127.0.0.1:5090",
            "/openapi": "http://127.0.0.1:5090",
        },
    },
    test: {
        environment: "jsdom",
        setupFiles: ["./src/test-setup.ts"],
        include: ["src/**/*.test.ts", "src/**/*.test.tsx"],
    },
});
