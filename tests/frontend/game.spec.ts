import{test,expect}from'../../frontend/node_modules/@playwright/test';import traces from'../fixtures/engine-bonus-cycle.json';
const amount=(n:number)=>new Intl.NumberFormat('en-US',{minimumFractionDigits:2,maximumFractionDigits:2}).format(n/100);
test('real API round, event playback, sound, rules and reload',async({page})=>{const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));await page.goto('/');await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();await expect(page.locator('canvas')).toBeVisible();await page.getByRole('button',{name:'Increase stake'}).click();await expect(page.getByTestId('stake')).toHaveText('50.00');const response=page.waitForResponse(r=>r.url().endsWith('/rounds')&&r.request().method()==='POST');await page.getByRole('button',{name:'Spin',exact:true}).click();const r=await(await response).json();await page.getByRole('button',{name:'Skip animation'}).click();await expect(page.getByTestId('balance')).toHaveText(amount(r.balanceAfter));await page.getByRole('button',{name:'Game rules'}).click();await expect(page.getByRole('dialog')).toContainText('Storm Orbs');await page.getByRole('button',{name:'Close dialog'}).click();await page.getByRole('button',{name:'Unmute sound'}).click();await page.getByRole('button',{name:'Mute sound'}).click();await page.reload();await expect(page.getByTestId('balance')).toHaveText(amount(r.balanceAfter));await expect(page.getByRole('button',{name:'Unmute sound'})).toBeVisible();expect(errors).toEqual([]);await page.screenshot({path:'../reports/thunderfall-desktop.png',fullPage:true});});
test('ambiguous response recovers one durable operation',async({page})=>{await page.goto('/');await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();let key:string|undefined;await page.route('**/api/v1/rounds',async route=>{key=route.request().postDataJSON().idempotencyKey;await route.fetch();await route.abort('failed')},{times:1});await page.getByRole('button',{name:'Spin',exact:true}).click();await expect(page.getByRole('alert')).toContainText('Connection interrupted');const response=page.waitForResponse(r=>r.url().endsWith('/rounds'));await page.getByRole('button',{name:'Retry round'}).click();const reply=await response;expect(reply.request().postDataJSON().idempotencyKey).toBe(key);const r=await reply.json();await page.getByRole('button',{name:'Skip animation'}).click();await expect(page.getByTestId('balance')).toHaveText(amount(r.balanceAfter));const history=page.waitForResponse(r=>r.url().includes('/rounds/history'));await page.getByRole('button',{name:'Round history'}).click();expect((await(await history).json()).items).toHaveLength(1);});
test('mobile, turbo, read-only replay, reset and Classic module',async({page})=>{await page.setViewportSize({width:390,height:844});await page.goto('/');await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();await page.getByRole('button',{name:/TURBO/}).click();await page.getByRole('button',{name:'Spin',exact:true}).click();await expect(page.getByRole('button',{name:'Skip animation'})).toHaveCount(0);const balance=await page.getByTestId('balance').textContent();await page.getByRole('button',{name:'REPLAY ROUND'}).click();await expect(page.getByRole('status')).toContainText('Replay complete');await expect(page.getByTestId('balance')).toHaveText(balance!);await page.screenshot({path:'../reports/thunderfall-mobile.png',fullPage:true});expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);await page.getByRole('button',{name:'RESTORE CREDITS'}).click();await page.getByRole('button',{name:'Restore 10,000 credits'}).click();await expect(page.getByTestId('balance')).toHaveText('10,000.00');await page.getByRole('combobox',{name:'Choose game'}).selectOption('classic');await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Spin',exact:true}).click();await expect(page.getByRole('button',{name:'Skip animation'})).toHaveCount(0);});
// Actual C# engine trace, not invented math. Browser-only routing never exists in production API.
test('recorded engine bonus/cascade trace plays and resumes after reload',async({page})=>{await page.goto('/');await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();let currentState=traces.rounds[0].startingMathState;await page.route('**/api/v1/sessions/current',async route=>{const original=await(await route.fetch()).json();await route.fulfill({json:{...original,featureState:currentState}})});let index=0;await page.route('**/api/v1/rounds',async route=>{const o=traces.rounds[index++];if(index>1)expect(route.request().postDataJSON().stake).toBeUndefined();currentState=o.finalMathState;await route.fulfill({json:{roundId:o.roundId,gameId:o.gameId,mathVersion:o.mathVersion,mode:o.mode,stake:o.stake,initialBoard:o.initialBoard,cascadeSteps:o.cascadeSteps,winningGroups:o.winningGroups,multiplierEvents:o.multiplierEvents,rawWin:o.rawWin,finalWin:o.finalWin,balanceAfter:1000000,featureState:o.finalMathState,presentationHints:index===1?[{type:'BonusIntro'}]:[],outcome:o}})});await page.getByRole('button',{name:'Spin',exact:true}).click();await expect(page.getByRole('button',{name:'Free spin',exact:true})).toBeEnabled();await expect(page.getByRole('button',{name:'Increase stake'})).toBeDisabled();await page.reload();await expect(page.getByRole('button',{name:'Free spin',exact:true})).toBeEnabled();await page.getByRole('button',{name:/TURBO/}).click();await page.getByRole('button',{name:'Free spin',exact:true}).click();await expect(page.getByRole('button',{name:'Skip animation'})).toHaveCount(0);await page.getByRole('button',{name:'Free spin',exact:true}).click();await expect(page.getByRole('button',{name:'Skip animation'})).toHaveCount(0);await expect(page.getByTestId('multiplier')).toHaveText('\u00d7'+traces.rounds[2].finalMathState.bonus!.persistentMultiplier);await page.screenshot({path:'../reports/thunderfall-bonus.png',fullPage:true});});

test('embedded game paginates the complete archive and buys a durable bonus', async ({page}) => {
    await page.goto('/');
    await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();
    const headers = {'X-Thunderfall-Client':'web'};
    await page.request.post('/api/v1/sessions/game',{headers,data:{gameId:'classic'}});
    for(let i=0;i<43;i++) {
        const response = await page.request.post('/api/v1/rounds',{headers,data:{stake:1000,idempotencyKey:crypto.randomUUID()}});
        expect(response.ok()).toBe(true);
    }
    await page.request.post('/api/v1/sessions/game',{headers,data:{gameId:'thunderfall'}});
    await page.reload();
    await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();
    const game = page.getByRole('main',{name:'Game window'});
    await expect(game.getByRole('button',{name:'Round history'})).toBeVisible();
    await game.getByRole('button',{name:'Round history'}).click();
    await expect(page.getByText('Page 1 of 3')).toBeVisible();
    await page.getByRole('button',{name:'Next page'}).click();
    await expect(page.getByText('Page 2 of 3')).toBeVisible();
    await page.getByRole('button',{name:'Next page'}).click();
    await expect(page.getByText('Page 3 of 3')).toBeVisible();
    await expect(page.getByRole('button',{name:'Next page'})).toBeDisabled();
    await page.screenshot({path:'../reports/thunderfall-history.png',fullPage:true});
    const balanceBeforeReplay = await page.getByTestId('balance').textContent();
    await page.getByRole('button',{name:/^Replay /}).first().click();
    await expect(page.getByRole('status')).toContainText('Replay complete');
    await expect(page.getByRole('combobox',{name:'Choose game'})).toHaveValue('classic');
    await expect(page.getByTestId('balance')).toHaveText(balanceBeforeReplay!);
    await page.getByRole('combobox',{name:'Choose game'}).selectOption('thunderfall');
    await expect(page.getByRole('button',{name:'Spin',exact:true})).toBeEnabled();
    await game.getByRole('button',{name:/BUY BONUS/}).click();
    await expect(page.getByRole('dialog')).toContainText('910.00');
    await page.getByRole('button',{name:'Buy & play first free spin'}).click();
    await expect(page.getByRole('button',{name:'Free spin',exact:true})).toBeEnabled({timeout:15000});
    await page.reload();
    await expect(page.getByRole('button',{name:'Free spin',exact:true})).toBeEnabled();
    const history = await (await page.request.get('/api/v1/rounds/history/page')).json();
    expect(history.total).toBe(44);
    expect(history.items[0].mode).toBe('BonusBuy');
    await page.screenshot({path:'../reports/thunderfall-purchased-bonus.png',fullPage:true});
});
