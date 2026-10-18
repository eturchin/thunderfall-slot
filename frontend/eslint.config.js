import tseslint from "typescript-eslint";
export default tseslint.config(...tseslint.configs.recommended, {
    files: ["src/**/*.{ts,tsx}"],
    languageOptions: {
        globals: {
            window: "readonly",
            document: "readonly",
            localStorage: "readonly",
            fetch: "readonly",
            performance: "readonly",
            requestAnimationFrame: "readonly",
            cancelAnimationFrame: "readonly",
            matchMedia: "readonly",
            devicePixelRatio: "readonly",
            AudioContext: "readonly",
            crypto: "readonly",
        },
    },
    rules: {
        "@typescript-eslint/no-unused-vars": [
            "error",
            { argsIgnorePattern: "^_" },
        ],
    },
});
