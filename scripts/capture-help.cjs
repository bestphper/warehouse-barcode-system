const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs = require('node:fs');
const path = require('node:path');

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  const root = process.env.HELP_URL || 'http://127.0.0.1:5174/react';
  const output = path.join(__dirname, '../frontend/public/help');
  fs.mkdirSync(output, { recursive: true });
  await page.goto(`${root}/`);
  await page.locator('.captcha-image').waitFor();
  await page.screenshot({ path: `${output}/quick-start.png` });
  const source = await page.locator('.captcha-image').getAttribute('src');
  const code = Buffer.from(source.split(',')[1], 'base64').toString().match(/>([A-Z0-9]{4})<\/text>/)[1];
  await page.locator('#username').fill('admin');
  await page.locator('#password').fill('admin');
  await page.locator('#captchaCode input, input#captchaCode').first().fill(code);
  await page.getByRole('button', { name: '登录系统' }).click();
  await page.waitForURL('**/dashboard');
  for (const [section, route] of Object.entries({ dashboard: 'dashboard', inventory: 'inventory', documents: 'documents', transfers: 'transfers', stocktakes: 'stocktakes', reports: 'reports', organization: 'organization', security: 'account', demo: 'dashboard', operations: 'account' })) {
    await page.goto(`${root}/${route}`);
    await page.waitForTimeout(1400);
    await page.screenshot({ path: `${output}/${section}.png`, fullPage: true });
    const detail = page.getByRole('button', { name: '查看详情' }).first();
    if (await detail.count()) {
      await detail.click();
      await page.locator('.ant-modal-content').waitFor();
      await page.waitForTimeout(300);
      await page.screenshot({ path: `${output}/${section}-detail.png` });
    }
  }
  for (const section of ['quick-start', 'dashboard', 'inventory', 'documents', 'transfers', 'stocktakes', 'reports', 'organization', 'security', 'operations', 'demo']) {
    await page.goto(`${root}/help/${section}`);
    await page.locator('.help-article').waitFor();
    await page.waitForFunction(() => [...document.querySelectorAll('.help-article img')].every(img => img.complete && img.naturalWidth > 0));
  }
  await page.screenshot({ path: '/private/tmp/warehouse-help-desktop.png' });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${root}/help/documents`);
  await page.locator('.help-mobile-directory').waitFor({ state: 'visible' });
  await page.screenshot({ path: '/private/tmp/warehouse-help-mobile.png', fullPage: true });
  if (await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)) throw new Error('Mobile help page overflows');
  await browser.close();
})().catch(error => { console.error(error); process.exit(1); });
