import { defineConfig, devices } from '@playwright/test'

const baseURL = process.env.QA_BASE_URL ?? 'http://localhost:8080'
const watching = process.env.QA_ASSISTIR === '1'

export default defineConfig({
  testDir: './tests',
  timeout: watching ? 180_000 : 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  outputDir: '../reports/playwright-arquivos',
  reporter: [
    ['list'],
    ['html', { outputFolder: '../reports/playwright', open: 'never' }],
    ['json', { outputFile: '../reports/playwright-resultado.json' }],
  ],
  use: {
    baseURL,
    locale: 'pt-BR',
    timezoneId: 'America/Sao_Paulo',
    viewport: { width: 1366, height: 820 },
    video: 'on',
    screenshot: 'on',
    trace: 'on',
    headless: !watching,
    launchOptions: { slowMo: watching ? 700 : 0 },
  },
  projects: [{ name: 'Chrome', use: { ...devices['Desktop Chrome'], viewport: { width: 1366, height: 820 } } }],
})
