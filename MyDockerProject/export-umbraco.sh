#!/bin/bash
set -e

# Get database password from .env or use default
if [ -f .env ]; then
    source .env
    DB_PASSWORD="${DB_PASSWORD:-Password1234}"
else
    DB_PASSWORD="${1:-Password1234}"
fi

# Create export directory with timestamp
EXPORT_DIR="../umbraco-export-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$EXPORT_DIR"

echo "=== Umbraco Export Script ==="
echo "Export directory: $EXPORT_DIR"
echo ""

# Step 1: Export database
echo "Exporting database..."
if docker exec mydockerproject_database /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$DB_PASSWORD" -d umbracoDb -C -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION" 2>/dev/null || \
   docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$DB_PASSWORD" -d umbracoDb -C -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION" 2>/dev/null; then
    echo "Database backup created in container"
    
    # Copy backup from container
    if docker cp mydockerproject_database:/var/opt/mssql/umbracoDb.bak "$EXPORT_DIR/umbracoDb.bak"; then
        echo "Database backup exported: $EXPORT_DIR/umbracoDb.bak"
    else
        echo "Warning: Failed to copy database backup from container"
    fi
else
    echo "Warning: Failed to create database backup"
fi

# Step 2: Export media files
echo ""
echo "Exporting media files..."
MEDIA_SOURCE="MyDockerProject/wwwroot/media"
MEDIA_DEST="$EXPORT_DIR/media"
if [ -d "$MEDIA_SOURCE" ]; then
    mkdir -p "$MEDIA_DEST"
    cp -r "$MEDIA_SOURCE/"* "$MEDIA_DEST/" 2>/dev/null || true
    echo "Media files exported: $MEDIA_DEST"
else
    echo "Warning: Media directory not found: $MEDIA_SOURCE"
fi

# Step 3: Export views
echo ""
echo "Exporting views..."
VIEWS_SOURCE="MyDockerProject/Views"
VIEWS_DEST="$EXPORT_DIR/Views"
if [ -d "$VIEWS_SOURCE" ]; then
    mkdir -p "$VIEWS_DEST"
    cp -r "$VIEWS_SOURCE/"* "$VIEWS_DEST/" 2>/dev/null || true
    echo "Views exported: $VIEWS_DEST"
else
    echo "Warning: Views directory not found: $VIEWS_SOURCE"
fi

# Step 4: Create manifest
echo ""
echo "Creating manifest..."
cat > "$EXPORT_DIR/manifest.json" <<EOF
{
  "exportDate": "$(date -Iseconds)",
  "exportType": "umbraco-full",
  "database": "umbracoDb.bak",
  "media": "media",
  "views": "Views",
  "version": "1.0"
}
EOF
echo "Manifest created: $EXPORT_DIR/manifest.json"

# Step 5: Create compressed archive (optional)
echo ""
read -p "Create compressed archive? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    ARCHIVE_NAME="${EXPORT_DIR}.tar.gz"
    echo "Creating archive: $ARCHIVE_NAME"
    tar -czf "$ARCHIVE_NAME" -C "$(dirname $EXPORT_DIR)" "$(basename $EXPORT_DIR)"
    echo "Archive created: $ARCHIVE_NAME"
    echo ""
    echo "You can now transfer this file to the destination server:"
    echo "  scp $ARCHIVE_NAME user@destination:/path/to/destination/"
fi

echo ""
echo "=== Export Complete ==="
echo "Export location: $EXPORT_DIR"
echo ""
echo "To import on another server:"
echo "  ./import-umbraco.sh $EXPORT_DIR [db-password]"
if [ -f "${EXPORT_DIR}.tar.gz" ]; then
    echo "  # Or from archive:"
    echo "  ./import-umbraco.sh ${EXPORT_DIR}.tar.gz [db-password]"
fi
