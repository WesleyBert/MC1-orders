import { existsSync } from 'node:fs'
import { chromium } from '@playwright/test'

const reportFile = process.argv[2]
const dashboardUrl = 'http://localhost:5665'
const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms))
const deadline = Date.now() + 4 * 60 * 1000

const browser = await chromium.launch({ headless: false, args: ['--start-maximized'] })
const page = await browser.newPage({ viewport: null })

while (Date.now() < deadline) {
  try {
    await page.goto(dashboardUrl, { timeout: 2000 })
    break
  } catch {
    await wait(1000)
  }
}

while (Date.now() < deadline && !existsSync(reportFile)) {
  await wait(1000)
}

await wait(3000)
await browser.close()
