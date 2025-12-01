#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <export-path-or-zip> [db-password]"
    echo "Example: $0 ../umbraco-export-20250118-120000 YourPassword123!"
    exit 1
fi

EXPORT_PATH="$1"
DB_PASSWORD="${2:-YourPassword123!}"

echo "=== Umbraco Import Script ==="

# Check if export path exists
if [ ! -e "$EXPORT_PATH" ]; then
    echo "Error: Export path not found: $EXPORT_PATH"
    exit 1
fi

# If it's a compressed file, extract it
if [[ "$EXPORT_PATH" == *.tar.gz ]] || [[ "$EXPORT_PATH" == *.tgz ]]; then
    echo "Extracting TAR archive..."
    extractDir="${EXPORT_PATH%.tar.gz}"
    extractDir="${extractDir%.tgz}"
    tar -xzf "$EXPORT_PATH" -C "$(dirname $EXPORT_PATH)"
    EXPORT_PATH="$extractDir"
    echo "Extracted to: $EXPORT_PATH"
elif [[ "$EXPORT_PATH" == *.zip ]]; then
    echo "Extracting ZIP archive..."
    extractDir="${EXPORT_PATH%.zip}"
    unzip -q "$EXPORT_PATH" -d "$(dirname $EXPORT_PATH)"
    EXPORT_PATH="$extractDir"
    echo "Extracted to: $EXPORT_PATH"
fi

# Verify it's a directory now
if [ ! -d "$EXPORT_PATH" ]; then
    echo "Error: Export path is not a directory: $EXPORT_PATH"
    exit 1
fi

# Step 1: Stop Umbraco
echo ""
echo "Stopping Umbraco container..."
if docker-compose stop mydockerproject 2>/dev/null; then
    echo "Umbraco stopped"
else
    echo "Warning: Could not stop Umbraco container (may not be running)"
fi

# Step 2: Restore database
echo ""
echo "Restoring database..."
BACKUP_FILE="$EXPORT_PATH/umbracoDb.bak"
if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: Database backup not found: $BACKUP_FILE"
    exit 1
fi

if docker cp "$BACKUP_FILE" mydockerproject_database:/var/opt/mssql/umbracoDb.bak && \
   docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -Q "RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY"; then
    echo "Database restored"
else
    echo "Error: Failed to restore database"
    echo "Make sure the database container is running and password is correct"
    exit 1
fi

# Step 3: Restore media
echo ""
echo "Restoring media files..."
MEDIA_SOURCE="$EXPORT_PATH/media"
MEDIA_DEST="MyDockerProject/wwwroot/media"
if [ -d "$MEDIA_SOURCE" ]; then
    mkdir -p "$MEDIA_DEST"
    cp -r "$MEDIA_SOURCE/"* "$MEDIA_DEST/" 2>/dev/null || true
    # Set permissions
    chmod -R 755 "$MEDIA_DEST" 2>/dev/null || true
    echo "Media files restored"
else
    echo "Warning: Media directory not found in export"
fi

# Step 4: Restore views
echo ""
echo "Restoring views..."
VIEWS_SOURCE="$EXPORT_PATH/Views"
if [ -d "$VIEWS_SOURCE" ]; then
    # Backup existing views first
    if [ -d "MyDockerProject/Views" ]; then
        backupViews="MyDockerProject/Views.backup-$(date +%Y%m%d-%H%M%S)"
        cp -r "MyDockerProject/Views" "$backupViews"
        echo "Existing views backed up to: $backupViews"
    fi
    cp -r "$VIEWS_SOURCE/"* "MyDockerProject/Views/"
    echo "Views restored"
else
    echo "Warning: Views directory not found in export"
fi

# Step 5: Set permissions
echo ""
echo "Setting permissions..."
chmod -R 755 "MyDockerProject/wwwroot" 2>/dev/null || true
chmod -R 755 "MyDockerProject/wwwroot/media" 2>/dev/null || true

# Try to set ownership (may fail if not root, that's OK)
if [ "$EUID" -eq 0 ]; then
    chown -R $(id -u):$(id -g) "MyDockerProject/wwwroot" 2>/dev/null || true
fi

# Step 6: Restart services
echo ""
echo "Restarting services..."
if docker-compose restart; then
    echo "Services restarted"
else
    echo "Warning: Could not restart services"
    echo "Try manually: docker-compose restart"
fi

echo ""
echo "=== Import Complete ==="
echo "Access Umbraco Backoffice: https://localhost:44372/umbraco"
echo ""
echo "Next steps:"
echo "1. Login to Umbraco backoffice"
echo "2. Verify content tree shows all content"
echo "3. Check Settings → Document Types (should all exist)"
echo "4. Check Media library (should show all files)"
echo "5. If homepage is missing, create it and set as start page"

