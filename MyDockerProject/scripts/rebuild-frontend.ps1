# Quick rebuild script - run this after making frontend changes
# Much faster than rebuilding Docker

Write-Host "`n=== Rebuilding Frontend ===" -ForegroundColor Cyan
Set-Location ..\frontend

Write-Host "Building..." -ForegroundColor Yellow
npm run build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Copying to wwwroot..." -ForegroundColor Yellow
    $targetDir = "..\MyDockerProject\MyDockerProject\wwwroot\scripts"
    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    Copy-Item -Force "dist\booking-engine.iife.js" "$targetDir\booking-engine.js"
    Write-Host "`n✓ Done! Refresh your browser (Ctrl+Shift+R) to see changes." -ForegroundColor Green
} else {
    Write-Host "`n✗ Build failed!" -ForegroundColor Red
    exit 1
}

Set-Location ..\MyDockerProject

