@echo off
cd /d "%~dp0playwright"
set QA_ASSISTIR=1
npx playwright test
