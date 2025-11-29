# Quick script to rebuild frontend and copy to wwwroot for development
# This allows fast iteration without rebuilding Docker

Write-Host "Building React booking engine..." -ForegroundColor Cyan
Set-Location ..\frontend

npm run build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Copying to wwwroot..." -ForegroundColor Green
    New-Item -ItemType Directory -Force -Path "..\MyDockerProject\MyDockerProject\wwwroot\scripts" | Out-Null
    Copy-Item -Force "dist\booking-engine.iife.js" "..\MyDockerProject\MyDockerProject\wwwroot\scripts\booking-engine.js"
    Write-Host "Done! Refresh your browser to see changes." -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Set-Location ..\MyDockerProject

