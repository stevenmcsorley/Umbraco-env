# Watch script that rebuilds frontend on file changes
# Uses PowerShell file watcher

Write-Host "Starting frontend file watcher..." -ForegroundColor Cyan
Write-Host "Watching: ..\frontend\src" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow

$frontendPath = Resolve-Path "..\frontend"
$srcPath = Join-Path $frontendPath "src"

$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $srcPath
$watcher.IncludeSubdirectories = $true
$watcher.EnableRaisingEvents = $true

$action = {
    $path = $Event.SourceEventArgs.FullPath
    $changeType = $Event.SourceEventArgs.ChangeType
    Write-Host "`n[$changeType] $path" -ForegroundColor Cyan
    
    Start-Sleep -Seconds 1 # Debounce
    
    Write-Host "Rebuilding..." -ForegroundColor Yellow
    Set-Location $frontendPath
    npm run build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Copying to wwwroot..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Force -Path "..\MyDockerProject\MyDockerProject\wwwroot\scripts" | Out-Null
        Copy-Item -Force "dist\booking-engine.iife.js" "..\MyDockerProject\MyDockerProject\wwwroot\scripts\booking-engine.js"
        Write-Host "Done! Refresh browser to see changes." -ForegroundColor Green
    } else {
        Write-Host "Build failed!" -ForegroundColor Red
    }
    
    Set-Location "..\MyDockerProject"
}

Register-ObjectEvent -InputObject $watcher -EventName "Changed" -Action $action | Out-Null
Register-ObjectEvent -InputObject $watcher -EventName "Created" -Action $action | Out-Null
Register-ObjectEvent -InputObject $watcher -EventName "Deleted" -Action $action | Out-Null

try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    $watcher.Dispose()
    Write-Host "`nWatcher stopped." -ForegroundColor Yellow
}

