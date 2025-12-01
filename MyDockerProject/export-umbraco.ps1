param(
    [string]$DbPassword = "YourPassword123!"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Umbraco Export Script ===" -ForegroundColor Green

# Step 1: Create export directory
$exportDir = "..\umbraco-export-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $exportDir -Force | Out-Null
Write-Host "Export directory: $exportDir" -ForegroundColor Cyan

# Step 2: Export database
Write-Host "`nExporting database..." -ForegroundColor Yellow
try {
    docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd `
        -S localhost -U sa -P $DbPassword `
        -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION"
    
    docker cp mydockerproject_database:/var/opt/mssql/umbracoDb.bak "$exportDir\umbracoDb.bak"
    Write-Host "Database exported" -ForegroundColor Green
} catch {
    Write-Host "Error exporting database: $_" -ForegroundColor Red
    Write-Host "Make sure the database container is running and password is correct" -ForegroundColor Yellow
    exit 1
}

# Step 3: Export media
Write-Host "`nExporting media files..." -ForegroundColor Yellow
$mediaSource = "MyDockerProject\wwwroot\media"
if (Test-Path $mediaSource) {
    Copy-Item -Path $mediaSource -Destination "$exportDir\media" -Recurse -Force
    Write-Host "Media files exported" -ForegroundColor Green
} else {
    Write-Host "Warning: Media directory not found at $mediaSource" -ForegroundColor Yellow
}

# Step 4: Export views
Write-Host "`nExporting views..." -ForegroundColor Yellow
$viewsSource = "MyDockerProject\Views"
if (Test-Path $viewsSource) {
    Copy-Item -Path $viewsSource -Destination "$exportDir\Views" -Recurse -Force
    Write-Host "Views exported" -ForegroundColor Green
} else {
    Write-Host "Warning: Views directory not found at $viewsSource" -ForegroundColor Yellow
}

# Step 5: Create manifest
$manifest = @{
    ExportDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    UmbracoVersion = "14.x"
    DatabaseBackup = "umbracoDb.bak"
    MediaFiles = "media"
    Views = "Views"
    Notes = "Complete Umbraco export including database, media, and views"
} | ConvertTo-Json -Depth 10

$manifest | Out-File -FilePath "$exportDir\manifest.json" -Encoding UTF8

# Step 6: Create ZIP archive (optional)
Write-Host "`nCreating compressed archive..." -ForegroundColor Yellow
try {
    Compress-Archive -Path "$exportDir\*" -DestinationPath "$exportDir.zip" -Force
    Write-Host "Compressed archive created: $exportDir.zip" -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not create ZIP archive: $_" -ForegroundColor Yellow
}

Write-Host "`n=== Export Complete ===" -ForegroundColor Green
Write-Host "Export location: $exportDir" -ForegroundColor Cyan
Write-Host "Files:" -ForegroundColor Cyan
Write-Host "  - umbracoDb.bak (database backup)" -ForegroundColor White
Write-Host "  - media/ (media files)" -ForegroundColor White
Write-Host "  - Views/ (custom views)" -ForegroundColor White
Write-Host "  - manifest.json (export info)" -ForegroundColor White
if (Test-Path "$exportDir.zip") {
    Write-Host "  - $exportDir.zip (compressed archive)" -ForegroundColor White
}

