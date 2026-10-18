import { defineConfig, devices } from "@playwright/test";
process.env.NO_PROXY = [process.env.NO_PROXY, "127.0.0.1", "localhost"]
    .filter(Boolean)
    .join(",");
export default defineConfig({
    testDir: "../tests/frontend",
    workers: 1,
    timeout: process.env.CI ? 90000 : 45000,
    // Software WebGL on CI needs time to finish real cascade/replay playback.
    expect: { timeout: 15000 },
    reporter: [["list"], ["html", { open: "never" }]],
    use: {
        baseURL: "http://127.0.0.1:5175",
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
        launchOptions: {
            args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"],
        },
    },
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
    webServer: {
        command: process.env.E2E_PREVIEW
            ? "npm run preview -- --port 5175"
            : "npm run dev",
        url: "http://127.0.0.1:5175",
        reuseExistingServer: !process.env.CI,
    },
});
