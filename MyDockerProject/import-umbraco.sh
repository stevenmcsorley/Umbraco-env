#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <export-path-or-zip> [db-password]"
    echo "Example: $0 ../umbraco-export-20250118-120000 YourPassword123!"
    exit 1
fi

EXPORT_PATH="$1"
# Get password from .env or use provided/default
if [ -f .env ]; then
    source .env
    DB_PASSWORD="${2:-${DB_PASSWORD:-Password1234}}"
else
    DB_PASSWORD="${2:-Password1234}"
fi

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
if docker compose stop mydockerproject 2>/dev/null || docker-compose stop mydockerproject 2>/dev/null; then
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

# Copy backup file to container
echo "Copying backup file to container..."
if ! docker cp "$BACKUP_FILE" mydockerproject_database:/var/opt/mssql/umbracoDb.bak; then
    echo "Error: Failed to copy backup file to container"
    exit 1
fi

# Try to find sqlcmd in various locations
echo "Locating sqlcmd..."
SQLCMD_PATH=""
for path in "sqlcmd" "/opt/mssql-tools/bin/sqlcmd" "/opt/mssql-tools18/bin/sqlcmd" "/opt/mssql-tools17/bin/sqlcmd" "/usr/local/bin/sqlcmd"; do
    if docker exec mydockerproject_database test -f "$path" 2>/dev/null || docker exec mydockerproject_database command -v "$path" >/dev/null 2>&1; then
        SQLCMD_PATH="$path"
        echo "Found sqlcmd at: $SQLCMD_PATH"
        break
    fi
done

# If sqlcmd not found, try to install it
if [ -z "$SQLCMD_PATH" ]; then
    echo "sqlcmd not found, attempting to install mssql-tools18..."
    if docker exec mydockerproject_database bash -c "apt-get update -qq && apt-get install -y -qq curl gnupg2 >/dev/null 2>&1 && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && curl -fsSL https://packages.microsoft.com/config/ubuntu/20.04/prod.list > /etc/apt/sources.list.d/mssql-release.list && apt-get update -qq && ACCEPT_EULA=Y DEBIAN_FRONTEND=noninteractive apt-get install -y -qq mssql-tools18 >/dev/null 2>&1 && test -f /opt/mssql-tools18/bin/sqlcmd"; then
        SQLCMD_PATH="/opt/mssql-tools18/bin/sqlcmd"
        echo "sqlcmd installed successfully"
    else
        echo "Warning: Could not install sqlcmd automatically"
        echo "You may need to restore the database manually"
        echo "Backup file location in container: /var/opt/mssql/umbracoDb.bak"
        echo ""
        echo "To restore manually, run:"
        echo "  docker exec -it mydockerproject_database bash"
        echo "  # Then install sqlcmd or use an alternative method"
        exit 1
    fi
fi

# Restore database using sqlcmd
echo "Restoring database..."
if docker exec mydockerproject_database $SQLCMD_PATH \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -C \
    -Q "RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY"; then
    echo "Database restored successfully"
else
    echo "Error: Failed to restore database"
    echo "Make sure the database container is running and password is correct"
    echo "You can try restoring manually:"
    echo "  docker exec -it mydockerproject_database $SQLCMD_PATH -S localhost -U sa -P '$DB_PASSWORD' -Q \"RESTORE DATABASE [umbracoDb] FROM DISK = '/var/opt/mssql/umbracoDb.bak' WITH REPLACE, RECOVERY\""
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
if docker compose restart 2>/dev/null || docker-compose restart 2>/dev/null; then
    echo "Services restarted"
else
    echo "Warning: Could not restart services"
    echo "Try manually: docker compose restart"
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

