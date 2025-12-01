param(
    [Parameter(Mandatory=$true)]
    [string]$ExportPath,
    [string]$DbPassword = "YourPassword123!"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Umbraco Import Script ===" -ForegroundColor Green

# Check if export path exists
if (-not (Test-Path $ExportPath)) {
    Write-Host "Error: Export path not found: $ExportPath" -ForegroundColor Red
    Write-Host "If you have a ZIP file, extract it first" -ForegroundColor Yellow
    exit 1
}

# If it's a ZIP file, extract it
if ($ExportPath -like "*.zip") {
    Write-Host "Extracting ZIP archive..." -ForegroundColor Yellow
    $extractDir = $ExportPath -replace '\.zip$', ''
    Expand-Archive -Path $ExportPath -DestinationPath (Split-Path $ExportPath) -Force
    $ExportPath = $extractDir
    Write-Host "Extracted to: $ExportPath" -ForegroundColor Green
}

# Step 1: Stop Umbraco
Write-Host "`nStopping Umbraco container..." -ForegroundColor Yellow
try {
    docker-compose stop mydockerproject
    Write-Host "Umbraco stopped" -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not stop Umbraco container (may not be running): $_" -ForegroundColor Yellow
}

# Step 2: Restore database
Write-Host "`nRestoring database..." -ForegroundColor Yellow
$backupFile = Join-Path $ExportPath "umbracoDb.bak"
if (-not (Test-Path $backupFile)) {
    Write-Host "Error: Database backup not found: $backupFile" -ForegroundColor Red
    exit 1
}

try {
    docker cp $backupFile mydockerproject_database:/var/opt/mssql/umbracoDb.bak
    docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd `
        -S localhost -U sa -P $DbPassword `
        -Q "RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY"
    Write-Host "Database restored" -ForegroundColor Green
} catch {
    Write-Host "Error restoring database: $_" -ForegroundColor Red
    Write-Host "Make sure the database container is running and password is correct" -ForegroundColor Yellow
    exit 1
}

# Step 3: Restore media
Write-Host "`nRestoring media files..." -ForegroundColor Yellow
$mediaSource = Join-Path $ExportPath "media"
$mediaDest = "MyDockerProject\wwwroot\media"
if (Test-Path $mediaSource) {
    New-Item -ItemType Directory -Path $mediaDest -Force | Out-Null
    Copy-Item -Path "$mediaSource\*" -Destination $mediaDest -Recurse -Force
    Write-Host "Media files restored" -ForegroundColor Green
} else {
    Write-Host "Warning: Media directory not found in export" -ForegroundColor Yellow
}

# Step 4: Restore views
Write-Host "`nRestoring views..." -ForegroundColor Yellow
$viewsSource = Join-Path $ExportPath "Views"
if (Test-Path $viewsSource) {
    # Backup existing views first
    if (Test-Path "MyDockerProject\Views") {
        $backupViews = "MyDockerProject\Views.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item -Path "MyDockerProject\Views" -Destination $backupViews -Recurse -Force
        Write-Host "Existing views backed up to: $backupViews" -ForegroundColor Cyan
    }
    Copy-Item -Path "$viewsSource\*" -Destination "MyDockerProject\Views" -Recurse -Force
    Write-Host "Views restored" -ForegroundColor Green
} else {
    Write-Host "Warning: Views directory not found in export" -ForegroundColor Yellow
}

# Step 5: Restart services
Write-Host "`nRestarting services..." -ForegroundColor Yellow
try {
    docker-compose restart
    Write-Host "Services restarted" -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not restart services: $_" -ForegroundColor Yellow
    Write-Host "Try manually: docker-compose restart" -ForegroundColor Yellow
}

Write-Host "`n=== Import Complete ===" -ForegroundColor Green
Write-Host "Access Umbraco Backoffice: https://localhost:44372/umbraco" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Login to Umbraco backoffice" -ForegroundColor White
Write-Host "2. Verify content tree shows all content" -ForegroundColor White
Write-Host "3. Check Settings → Document Types (should all exist)" -ForegroundColor White
Write-Host "4. Check Media library (should show all files)" -ForegroundColor White
Write-Host "5. If homepage is missing, create it and set as start page" -ForegroundColor White

