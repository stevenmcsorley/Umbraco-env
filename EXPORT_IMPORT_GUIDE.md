# Complete Export/Import Guide

This guide provides step-by-step instructions to export your complete Umbraco installation from one computer and import it on another, including all content, document types, media files, database, and settings.

## What Gets Exported

✅ **Umbraco Database** - All content, document types, data types, media references, settings  
✅ **Media Files** - All uploaded images and files from `wwwroot/media`  
✅ **Custom Views** - All Razor views and templates  
✅ **Database Tables** - Bookings, Inventory, Users (custom tables)  
✅ **Configuration** - Document types, data types, content structure  

## Prerequisites

### On Source Machine (Export)
- Docker and Docker Compose running
- SQL Server tools (or Docker access)
- PowerShell (Windows) or Bash (Linux/Mac)

### On Destination Machine (Import)
- Docker and Docker Compose installed
- Same prerequisites as source
- Git repository cloned

---

## PART 1: EXPORT FROM SOURCE MACHINE

### Step 1: Create Export Directory

**Windows (PowerShell):**
```powershell
cd MyDockerProject
$exportDir = "..\umbraco-export-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $exportDir -Force
```

**Linux/Mac (Bash):**
```bash
cd MyDockerProject
exportDir="../umbraco-export-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$exportDir"
```

### Step 2: Export SQL Server Database

**Windows (PowerShell):**
```powershell
# Get database password from .env or docker-compose
$dbPassword = "YourPassword123!"  # Change this to your actual password

# Create backup inside container
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd `
    -S localhost -U sa -P $dbPassword `
    -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION"

# Copy backup from container to host
docker cp mydockerproject_database:/var/opt/mssql/umbracoDb.bak "$exportDir\umbracoDb.bak"
```

**Linux/Mac (Bash):**
```bash
# Get database password from .env or docker-compose
DB_PASSWORD="YourPassword123!"  # Change this to your actual password

# Create backup inside container
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION"

# Copy backup from container to host
docker cp mydockerproject_database:/var/opt/mssql/umbracoDb.bak "$exportDir/umbracoDb.bak"
```

### Step 3: Export Media Files

**Windows (PowerShell):**
```powershell
# Copy media directory
$mediaSource = "MyDockerProject\wwwroot\media"
$mediaDest = "$exportDir\media"
if (Test-Path $mediaSource) {
    Copy-Item -Path $mediaSource -Destination $mediaDest -Recurse -Force
    Write-Host "Media files exported to: $mediaDest" -ForegroundColor Green
} else {
    Write-Host "Warning: Media directory not found at $mediaSource" -ForegroundColor Yellow
}
```

**Linux/Mac (Bash):**
```bash
# Copy media directory
mediaSource="MyDockerProject/wwwroot/media"
mediaDest="$exportDir/media"
if [ -d "$mediaSource" ]; then
    cp -r "$mediaSource" "$mediaDest"
    echo "Media files exported to: $mediaDest"
else
    echo "Warning: Media directory not found at $mediaSource"
fi
```

### Step 4: Export Custom Views (Optional but Recommended)

**Windows (PowerShell):**
```powershell
# Copy Views directory
$viewsSource = "MyDockerProject\Views"
$viewsDest = "$exportDir\Views"
if (Test-Path $viewsSource) {
    Copy-Item -Path $viewsSource -Destination $viewsDest -Recurse -Force
    Write-Host "Views exported to: $viewsDest" -ForegroundColor Green
}
```

**Linux/Mac (Bash):**
```bash
# Copy Views directory
viewsSource="MyDockerProject/Views"
viewsDest="$exportDir/Views"
if [ -d "$viewsSource" ]; then
    cp -r "$viewsSource" "$viewsDest"
    echo "Views exported to: $viewsDest"
fi
```

### Step 5: Create Export Manifest

**Windows (PowerShell):**
```powershell
$manifest = @{
    ExportDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    UmbracoVersion = "14.x"  # Update if known
    DatabaseBackup = "umbracoDb.bak"
    MediaFiles = "media"
    Views = "Views"
    Notes = "Complete Umbraco export including database, media, and views"
} | ConvertTo-Json -Depth 10

$manifest | Out-File -FilePath "$exportDir\manifest.json" -Encoding UTF8
Write-Host "`n=== Export Complete ===" -ForegroundColor Green
Write-Host "Export location: $exportDir" -ForegroundColor Cyan
Write-Host "Files:" -ForegroundColor Cyan
Write-Host "  - umbracoDb.bak (database backup)" -ForegroundColor White
Write-Host "  - media/ (media files)" -ForegroundColor White
Write-Host "  - Views/ (custom views)" -ForegroundColor White
Write-Host "  - manifest.json (export info)" -ForegroundColor White
```

**Linux/Mac (Bash):**
```bash
cat > "$exportDir/manifest.json" <<EOF
{
  "exportDate": "$(date -Iseconds)",
  "umbracoVersion": "14.x",
  "databaseBackup": "umbracoDb.bak",
  "mediaFiles": "media",
  "views": "Views",
  "notes": "Complete Umbraco export including database, media, and views"
}
EOF

echo ""
echo "=== Export Complete ==="
echo "Export location: $exportDir"
echo "Files:"
echo "  - umbracoDb.bak (database backup)"
echo "  - media/ (media files)"
echo "  - Views/ (custom views)"
echo "  - manifest.json (export info)"
```

### Step 6: Compress Export (Optional)

**Windows (PowerShell):**
```powershell
# Create ZIP archive
Compress-Archive -Path "$exportDir\*" -DestinationPath "$exportDir.zip" -Force
Write-Host "Compressed export created: $exportDir.zip" -ForegroundColor Green
```

**Linux/Mac (Bash):**
```bash
# Create TAR archive
tar -czf "$exportDir.tar.gz" -C "$(dirname $exportDir)" "$(basename $exportDir)"
echo "Compressed export created: $exportDir.tar.gz"
```

---

## PART 2: IMPORT TO DESTINATION MACHINE

**Note:** The export directory is included in the git repository, so you don't need to transfer files separately. Simply clone the repository and the export will be available.

### Quick Start (Recommended)

The easiest way to import is using the automated import script:

**Linux/Mac:**
```bash
# Clone repository
git clone <repository-url>
cd UmbraccoEnv/MyDockerProject

# Make script executable
chmod +x import-umbraco.sh

# Import (script handles everything automatically)
./import-umbraco.sh "../umbraco-export-20251201-225510" "Password1234"
```

**Windows:**
```powershell
# Clone repository
git clone <repository-url>
cd UmbraccoEnv/MyDockerProject

# Import (script handles everything automatically)
.\import-umbraco.ps1 -ExportPath "..\umbraco-export-20251201-225510" -DbPassword "Password1234"
```

The script will:
1. Stop Umbraco container
2. Restore database from backup
3. Copy all media files
4. Restore all views
5. Set correct permissions (Linux)
6. Restart services

**That's it!** Access Umbraco at `https://localhost:44372/umbraco`

---

### Manual Import Steps (If Needed)

If you prefer to import manually or need to troubleshoot, follow these steps:

### Step 1: Clone Repository and Start Services

```bash
# Clone repository (export directory is included)
git clone <repository-url>
cd UmbraccoEnv/MyDockerProject

# The export directory is now available at: ../umbraco-export-YYYYMMDD-HHMMSS/
# Or use the ZIP file: ../umbraco-export-YYYYMMDD-HHMMSS.zip

# Start services (but don't access Umbraco yet)
docker-compose up -d

# Wait for database to be ready
echo "Waiting for database to initialize..."
sleep 30
```

**Quick Import Option:** You can use the automated import script directly:
```bash
# Make scripts executable
chmod +x import-umbraco.sh

# Import using the export directory (recommended)
./import-umbraco.sh "../umbraco-export-20251201-225510" "Password1234"

# Or import using the ZIP file
./import-umbraco.sh "../umbraco-export-20251201-225510.zip" "Password1234"
```

The script will handle all the steps below automatically. If you prefer manual import, continue with the steps below.

### Step 2: Stop Umbraco Container (Important!)

**Windows (PowerShell):**
```powershell
docker-compose stop mydockerproject
```

**Linux/Mac (Bash):**
```bash
docker-compose stop mydockerproject
```

**Why?** We need to restore the database before Umbraco tries to connect to it.

### Step 3: Locate Export Directory

The export directory is in the repository root. Find the latest export:

**Windows (PowerShell):**
```powershell
# Find the export directory
$exportDir = Get-ChildItem -Path ".." -Directory -Filter "umbraco-export-*" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "Using export: $exportDir" -ForegroundColor Cyan
```

**Linux/Mac (Bash):**
```bash
# Find the export directory (or use the specific one from the repo)
exportDir="../umbraco-export-20251201-225510"  # Update with actual export name
# Or find the latest:
# exportDir=$(ls -td ../umbraco-export-* | head -1)
echo "Using export: $exportDir"
```

### Step 4: Restore Database

**Windows (PowerShell):**
```powershell
# Copy backup file into container
docker cp "$exportDir\umbracoDb.bak" mydockerproject_database:/var/opt/mssql/umbracoDb.bak

# Get database password (should match source or check .env file)
$dbPassword = "YourPassword123!"  # Change this to your actual password

# Restore database
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd `
    -S localhost -U sa -P $dbPassword `
    -Q "RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY"
```

**Linux/Mac (Bash):**
```bash
# Copy backup file into container
docker cp "$exportDir/umbracoDb.bak" mydockerproject_database:/var/opt/mssql/umbracoDb.bak

# Get database password (should match source or check .env file)
DB_PASSWORD="YourPassword123!"  # Change this to your actual password

# Restore database
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -Q "RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY"
```

**Important Notes:**
- `REPLACE` overwrites the existing database
- `RECOVERY` brings the database online immediately
- The database password must match what's in your `docker-compose.yml` or `.env` file

### Step 5: Restore Media Files

**Windows (PowerShell):**
```powershell
# Ensure media directory exists
$mediaDest = "MyDockerProject\wwwroot\media"
New-Item -ItemType Directory -Path $mediaDest -Force

# Copy media files
if (Test-Path "$exportDir\media") {
    Copy-Item -Path "$exportDir\media\*" -Destination $mediaDest -Recurse -Force
    Write-Host "Media files restored" -ForegroundColor Green
}

# Set permissions (if needed on Windows)
# Usually not needed on Windows, but if you get permission errors:
icacls $mediaDest /grant "Users:(OI)(CI)F" /T
```

**Linux/Mac (Bash):**
```bash
# Ensure media directory exists
mediaDest="MyDockerProject/wwwroot/media"
mkdir -p "$mediaDest"

# Copy media files
if [ -d "$exportDir/media" ]; then
    cp -r "$exportDir/media/"* "$mediaDest/"
    echo "Media files restored"
fi

# Set correct permissions (IMPORTANT on Linux)
chmod -R 755 "$mediaDest"
chown -R $(id -u):$(id -g) "$mediaDest" 2>/dev/null || true
```

### Step 6: Restore Custom Views (If Exported)

**Windows (PowerShell):**
```powershell
# Backup existing views first (optional)
if (Test-Path "MyDockerProject\Views") {
    Copy-Item -Path "MyDockerProject\Views" -Destination "MyDockerProject\Views.backup" -Recurse -Force
}

# Restore views
if (Test-Path "$exportDir\Views") {
    Copy-Item -Path "$exportDir\Views\*" -Destination "MyDockerProject\Views" -Recurse -Force
    Write-Host "Views restored" -ForegroundColor Green
}
```

**Linux/Mac (Bash):**
```bash
# Backup existing views first (optional)
if [ -d "MyDockerProject/Views" ]; then
    cp -r "MyDockerProject/Views" "MyDockerProject/Views.backup"
fi

# Restore views
if [ -d "$exportDir/Views" ]; then
    cp -r "$exportDir/Views/"* "MyDockerProject/Views/"
    echo "Views restored"
fi
```

### Step 7: Set Permissions on wwwroot (Linux Only)

**Linux/Mac (Bash):**
```bash
# Set permissions for wwwroot directory
wwwrootPath="MyDockerProject/wwwroot"
chmod -R 755 "$wwwrootPath"
chown -R $(id -u):$(id -g) "$wwwrootPath" 2>/dev/null || true

# Specifically for media subdirectory
chmod -R 755 "$wwwrootPath/media"
```

### Step 8: Restart Services

**Windows (PowerShell):**
```powershell
# Restart all services
docker-compose restart

# Or start Umbraco if it was stopped
docker-compose start mydockerproject
```

**Linux/Mac (Bash):**
```bash
# Restart all services
docker-compose restart

# Or start Umbraco if it was stopped
docker-compose start mydockerproject
```

### Step 9: Verify Import

1. **Check Database Connection:**
   ```bash
   # Test database connection
   docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd \
       -S localhost -U sa -P "$DB_PASSWORD" \
       -Q "SELECT COUNT(*) FROM umbracoContent"
   ```

2. **Access Umbraco Backoffice:**
   - Open: `https://localhost:44372/umbraco`
   - Login with the same credentials as source machine
   - Check Content tree - all content should be there
   - Check Settings → Document Types - all document types should exist
   - Check Media - all media files should be visible

3. **Verify Content:**
   ```bash
   # Test API endpoint
   curl http://localhost:44372/api/hotels
   ```

4. **Check Homepage:**
   - Go to Content in backoffice
   - Verify homepage exists and is set as start node
   - If homepage is missing, you may need to recreate it (see troubleshooting)

---

## Automated Scripts

### Complete Export Script (Windows)

Save as `export-umbraco.ps1`:

```powershell
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
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd `
    -S localhost -U sa -P $DbPassword `
    -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION"

docker cp mydockerproject_database:/var/opt/mssql/umbracoDb.bak "$exportDir\umbracoDb.bak"
Write-Host "Database exported" -ForegroundColor Green

# Step 3: Export media
Write-Host "`nExporting media files..." -ForegroundColor Yellow
$mediaSource = "MyDockerProject\wwwroot\media"
if (Test-Path $mediaSource) {
    Copy-Item -Path $mediaSource -Destination "$exportDir\media" -Recurse -Force
    Write-Host "Media files exported" -ForegroundColor Green
} else {
    Write-Host "Warning: Media directory not found" -ForegroundColor Yellow
}

# Step 4: Export views
Write-Host "`nExporting views..." -ForegroundColor Yellow
$viewsSource = "MyDockerProject\Views"
if (Test-Path $viewsSource) {
    Copy-Item -Path $viewsSource -Destination "$exportDir\Views" -Recurse -Force
    Write-Host "Views exported" -ForegroundColor Green
}

# Step 5: Create manifest
$manifest = @{
    ExportDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    UmbracoVersion = "14.x"
    DatabaseBackup = "umbracoDb.bak"
    MediaFiles = "media"
    Views = "Views"
} | ConvertTo-Json -Depth 10

$manifest | Out-File -FilePath "$exportDir\manifest.json" -Encoding UTF8

Write-Host "`n=== Export Complete ===" -ForegroundColor Green
Write-Host "Location: $exportDir" -ForegroundColor Cyan
```

**Usage:**
```powershell
cd MyDockerProject
.\export-umbraco.ps1 -DbPassword "YourActualPassword"
```

### Complete Import Script (Windows)

Save as `import-umbraco.ps1`:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$ExportPath,
    [string]$DbPassword = "YourPassword123!"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Umbraco Import Script ===" -ForegroundColor Green

if (-not (Test-Path $ExportPath)) {
    Write-Host "Error: Export path not found: $ExportPath" -ForegroundColor Red
    exit 1
}

# Step 1: Stop Umbraco
Write-Host "`nStopping Umbraco container..." -ForegroundColor Yellow
docker-compose stop mydockerproject

# Step 2: Restore database
Write-Host "`nRestoring database..." -ForegroundColor Yellow
$backupFile = Join-Path $ExportPath "umbracoDb.bak"
if (-not (Test-Path $backupFile)) {
    Write-Host "Error: Database backup not found: $backupFile" -ForegroundColor Red
    exit 1
}

docker cp $backupFile mydockerproject_database:/var/opt/mssql/umbracoDb.bak
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd `
    -S localhost -U sa -P $DbPassword `
    -Q "RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY"

Write-Host "Database restored" -ForegroundColor Green

# Step 3: Restore media
Write-Host "`nRestoring media files..." -ForegroundColor Yellow
$mediaSource = Join-Path $ExportPath "media"
$mediaDest = "MyDockerProject\wwwroot\media"
if (Test-Path $mediaSource) {
    New-Item -ItemType Directory -Path $mediaDest -Force | Out-Null
    Copy-Item -Path "$mediaSource\*" -Destination $mediaDest -Recurse -Force
    Write-Host "Media files restored" -ForegroundColor Green
}

# Step 4: Restore views
Write-Host "`nRestoring views..." -ForegroundColor Yellow
$viewsSource = Join-Path $ExportPath "Views"
if (Test-Path $viewsSource) {
    Copy-Item -Path "$viewsSource\*" -Destination "MyDockerProject\Views" -Recurse -Force
    Write-Host "Views restored" -ForegroundColor Green
}

# Step 5: Restart services
Write-Host "`nRestarting services..." -ForegroundColor Yellow
docker-compose restart

Write-Host "`n=== Import Complete ===" -ForegroundColor Green
Write-Host "Access Umbraco: https://localhost:44372/umbraco" -ForegroundColor Cyan
```

**Usage:**
```powershell
cd MyDockerProject
# Import from export directory in repository
.\import-umbraco.ps1 -ExportPath "..\umbraco-export-20251201-225510" -DbPassword "Password1234"

# Or import from ZIP file
.\import-umbraco.ps1 -ExportPath "..\umbraco-export-20251201-225510.zip" -DbPassword "Password1234"
```

### Complete Export Script (Linux/Mac)

Save as `export-umbraco.sh`:

```bash
#!/bin/bash
set -e

DB_PASSWORD="${1:-YourPassword123!}"

echo "=== Umbraco Export Script ==="

# Step 1: Create export directory
exportDir="../umbraco-export-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$exportDir"
echo "Export directory: $exportDir"

# Step 2: Export database
echo ""
echo "Exporting database..."
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION"

docker cp mydockerproject_database:/var/opt/mssql/umbracoDb.bak "$exportDir/umbracoDb.bak"
echo "Database exported"

# Step 3: Export media
echo ""
echo "Exporting media files..."
if [ -d "MyDockerProject/wwwroot/media" ]; then
    cp -r "MyDockerProject/wwwroot/media" "$exportDir/media"
    echo "Media files exported"
else
    echo "Warning: Media directory not found"
fi

# Step 4: Export views
echo ""
echo "Exporting views..."
if [ -d "MyDockerProject/Views" ]; then
    cp -r "MyDockerProject/Views" "$exportDir/Views"
    echo "Views exported"
fi

# Step 5: Create manifest
cat > "$exportDir/manifest.json" <<EOF
{
  "exportDate": "$(date -Iseconds)",
  "umbracoVersion": "14.x",
  "databaseBackup": "umbracoDb.bak",
  "mediaFiles": "media",
  "views": "Views"
}
EOF

echo ""
echo "=== Export Complete ==="
echo "Location: $exportDir"
```

**Usage:**
```bash
chmod +x export-umbraco.sh
cd MyDockerProject
./export-umbraco.sh "YourActualPassword"
```

### Complete Import Script (Linux/Mac)

Save as `import-umbraco.sh`:

```bash
#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <export-path> [db-password]"
    exit 1
fi

EXPORT_PATH="$1"
DB_PASSWORD="${2:-YourPassword123!}"

if [ ! -d "$EXPORT_PATH" ]; then
    echo "Error: Export path not found: $EXPORT_PATH"
    exit 1
fi

echo "=== Umbraco Import Script ==="

# Step 1: Stop Umbraco
echo ""
echo "Stopping Umbraco container..."
docker-compose stop mydockerproject

# Step 2: Restore database
echo ""
echo "Restoring database..."
BACKUP_FILE="$EXPORT_PATH/umbracoDb.bak"
if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: Database backup not found: $BACKUP_FILE"
    exit 1
fi

docker cp "$BACKUP_FILE" mydockerproject_database:/var/opt/mssql/umbracoDb.bak
docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -Q "RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY"

echo "Database restored"

# Step 3: Restore media
echo ""
echo "Restoring media files..."
MEDIA_SOURCE="$EXPORT_PATH/media"
MEDIA_DEST="MyDockerProject/wwwroot/media"
if [ -d "$MEDIA_SOURCE" ]; then
    mkdir -p "$MEDIA_DEST"
    cp -r "$MEDIA_SOURCE/"* "$MEDIA_DEST/"
    chmod -R 755 "$MEDIA_DEST"
    echo "Media files restored"
fi

# Step 4: Restore views
echo ""
echo "Restoring views..."
VIEWS_SOURCE="$EXPORT_PATH/Views"
if [ -d "$VIEWS_SOURCE" ]; then
    cp -r "$VIEWS_SOURCE/"* "MyDockerProject/Views/"
    echo "Views restored"
fi

# Step 5: Set permissions
echo ""
echo "Setting permissions..."
chmod -R 755 "MyDockerProject/wwwroot"
chmod -R 755 "MyDockerProject/wwwroot/media" 2>/dev/null || true

# Step 6: Restart services
echo ""
echo "Restarting services..."
docker-compose restart

echo ""
echo "=== Import Complete ==="
echo "Access Umbraco: https://localhost:44372/umbraco"
```

**Usage:**
```bash
chmod +x import-umbraco.sh
cd MyDockerProject
# Import from export directory in repository
./import-umbraco.sh "../umbraco-export-20251201-225510" "Password1234"

# Or import from ZIP file
./import-umbraco.sh "../umbraco-export-20251201-225510.zip" "Password1234"
```

---

## Troubleshooting

### Issue: Permission Denied on Media Files (Linux)

**Solution:**
```bash
sudo chown -R $(id -u):$(id -g) MyDockerProject/wwwroot/media
chmod -R 755 MyDockerProject/wwwroot/media
```

### Issue: Database Restore Fails with "Database in Use"

**Solution:**
```bash
# Stop Umbraco container first
docker-compose stop mydockerproject

# Then restore
# ... (restore commands)

# Then start again
docker-compose start mydockerproject
```

### Issue: Homepage Missing After Import

**Solution:**
1. Go to Umbraco Backoffice → Content
2. Right-click on root → Create
3. Create a "Home" page (or your homepage document type)
4. Go to Settings → Content → Right-click Home → "Set as start page"

### Issue: Document Types Partially Created

**Solution:**
This usually means the database restore didn't complete properly. Try:
1. Stop all services
2. Remove the database volume: `docker volume rm mydockerproject_umb_database`
3. Restart services to create fresh database
4. Restore again using the import script

### Issue: Media Files Not Showing in Backoffice

**Solution:**
1. Check file permissions (see above)
2. Check that files exist in `wwwroot/media`
3. Restart Umbraco container: `docker-compose restart mydockerproject`
4. Clear Umbraco cache in backoffice: Settings → Published Status → Rebuild

### Issue: "Cannot connect to database" After Import

**Solution:**
1. Check database password matches in `docker-compose.yml` or `.env`
2. Verify database container is running: `docker ps`
3. Check database logs: `docker logs mydockerproject_database`
4. Test connection: `docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPassword" -Q "SELECT 1"`

---

## Quick Reference

### Export Checklist
- [ ] Database backup created
- [ ] Media files copied
- [ ] Views copied (optional)
- [ ] Manifest created
- [ ] Export committed to git repository (or transferred to destination)

### Import Checklist
- [ ] Repository cloned (export directory is included)
- [ ] Services started (then Umbraco stopped)
- [ ] Export directory located (or use import script)
- [ ] Database restored
- [ ] Media files restored
- [ ] Permissions set (Linux)
- [ ] Services restarted
- [ ] Backoffice accessible
- [ ] Content verified
- [ ] Homepage set (if needed)

---

## Notes

- **Database Password**: Make sure the password matches between source and destination, or update `docker-compose.yml`/`.env` file. Default password is `Password1234`.
- **Export in Repository**: The export directory (`umbraco-export-20251201-225510`) is included in the git repository, so no manual file transfer is needed. Just clone the repo.
- **Port Conflicts**: If port 44372 is in use, change it in `docker-compose.yml`
- **Large Exports**: The current export is ~45MB. For very large media libraries, consider using Git LFS
- **Version Compatibility**: This guide assumes Umbraco 14.x. For other versions, adjust accordingly
- **Backup Before Import**: Always backup existing data before importing on destination machine

