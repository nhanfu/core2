const puppeteer = require('puppeteer');

const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    page.setDefaultTimeout(30000);

    // Capture ALL network requests
    page.on('request', request => {
        if (request.url().includes('/api/')) {
            console.log('[REQUEST]', request.method(), request.url());
        }
    });

    // Capture console logs
    page.on('console', msg => {
        console.log('[BROWSER CONSOLE]', msg.text());
    });

    // Capture all responses
    page.on('response', response => {
        const url = response.url();
        if (url.includes('/api/')) {
            console.log('[RESPONSE]', response.status(), url);
        }
    });

    try {
        // Step 1: Login
        console.log('=== Step 1: Login ===');
        await page.goto('http://localhost:5173', { waitUntil: 'networkidle2' });
        await sleep(2000);

        // Check what's on the page
        const pageContent = await page.content();
        console.log('Page title:', await page.title());

        // Debug: check Client.api value
        const apiUrl = await page.evaluate(() => window.Client?.api);
        console.log('Client.api:', apiUrl);

        // Fill login form - use evaluate to be more reliable
        await page.evaluate(() => {
            const tenantInput = document.querySelector('input[name="TenantCode"]');
            const userInput = document.querySelector('input[name="UserName"]');
            const passInput = document.querySelector('input[name="Password"]');
            if (tenantInput) {
                tenantInput.value = 'crm';
                tenantInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (userInput) {
                userInput.value = 'admin';
                userInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (passInput) {
                passInput.value = '123123';
                passInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
        });

        console.log('Form filled');

        // Click login button using evaluate
        await page.evaluate(() => {
            const btn = document.querySelector('.login-form-btn');
            if (btn) {
                btn.click();
            }
        });

        await sleep(5000);

        // Check if logged in
        const token = await page.evaluate(() => {
            const data = localStorage.getItem('UserInfo');
            return data;
        });
        console.log('Token after login:', token ? 'exists' : 'null');

        if (token) {
            const tokenData = JSON.parse(token);
            console.log('AccessTokenExp:', tokenData.AccessTokenExp);
            console.log('refreshTokenExp:', tokenData.refreshTokenExp);
        }

        // Step 2: Refresh the page to trigger token refresh
        console.log('\n=== Step 2: Refresh page ===');
        await page.reload({ waitUntil: 'networkidle2' });
        await sleep(3000);

        // Check token after refresh
        const tokenAfterRefresh = await page.evaluate(() => localStorage.getItem('UserInfo'));
        console.log('Token after refresh:', tokenAfterRefresh ? 'exists' : 'null');

    } catch (error) {
        console.error('Error:', error.message);
        console.error(error.stack);
    } finally {
        await browser.close();
    }
})();
