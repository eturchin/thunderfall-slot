import { test, expect } from "../../frontend/node_modules/@playwright/test";

test("session refresh failure retries the committed round after reload", async ({
    page,
}) => {
    await page.goto("/");
    await expect(
        page.getByRole("button", { name: "Spin", exact: true }),
    ).toBeEnabled();
    await page.getByRole("button", { name: /TURBO/ }).click();
    await page.route(
        "**/api/v1/sessions/current",
        async (route) => {
            await route.fulfill({
                status: 409,
                contentType: "application/problem+json",
                body: JSON.stringify({ code: "RefreshFailed" }),
            });
        },
        { times: 1 },
    );

    const firstResponse = page.waitForResponse(
        (response) =>
            response.url().endsWith("/rounds") &&
            response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Spin", exact: true }).click();
    const first = await firstResponse;
    const command = first.request().postDataJSON();
    const round = await first.json();
    await expect(page.getByRole("alert")).toContainText("RefreshFailed");

    await page.reload();
    await expect(
        page.getByRole("button", { name: "Retry round" }),
    ).toBeEnabled();
    const retryResponse = page.waitForResponse(
        (response) =>
            response.url().endsWith("/rounds") &&
            response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Retry round" }).click();
    const retry = await retryResponse;
    expect(retry.request().postDataJSON()).toEqual(command);
    expect((await retry.json()).roundId).toBe(round.roundId);
    await expect(
        page.getByRole("button", { name: "Skip animation" }),
    ).toHaveCount(0);

    const history = await (
        await page.request.get("/api/v1/rounds/history/page")
    ).json();
    expect(history.total).toBe(1);
    expect(history.items).toHaveLength(1);
    const current = await (
        await page.request.get("/api/v1/sessions/current")
    ).json();
    expect(current.balance).toBe(round.balanceAfter);
});

test("storage failure leaves retry usable without sending a round", async ({
    page,
}) => {
    const errors: string[] = [];
    const rounds: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    page.on("request", (request) => {
        if (request.method() === "POST" && request.url().endsWith("/rounds")) {
            rounds.push(request.url());
        }
    });
    await page.goto("/");
    await expect(
        page.getByRole("button", { name: "Spin", exact: true }),
    ).toBeEnabled();
    await page.evaluate(() => {
        const original = Storage.prototype.setItem;
        Storage.prototype.setItem = function (key, value) {
            if (key === "tf-pending") {
                throw new DOMException("Storage full", "QuotaExceededError");
            }
            original.call(this, key, value);
        };
    });

    await page.getByRole("button", { name: "Spin", exact: true }).click();
    await expect(page.getByRole("alert")).toContainText(
        "Could not save the round request",
    );
    await page.getByRole("button", { name: "Retry round" }).click();
    await expect(
        page.getByRole("button", { name: "Retry round" }),
    ).toBeEnabled();
    expect(rounds).toEqual([]);
    expect(errors).toEqual([]);
});
