@echo off
cd /d "%~dp0playwright"
npx playwright test --ui
