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
if docker exec mydockerproject_database /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -Q "BACKUP DATABASE [umbracoDb] TO DISK = '/var/opt/mssql/umbracoDb.bak' WITH FORMAT, INIT, COMPRESSION"; then
    docker cp mydockerproject_database:/var/opt/mssql/umbracoDb.bak "$exportDir/umbracoDb.bak"
    echo "Database exported"
else
    echo "Error: Failed to export database"
    echo "Make sure the database container is running and password is correct"
    exit 1
fi

# Step 3: Export media
echo ""
echo "Exporting media files..."
if [ -d "MyDockerProject/wwwroot/media" ]; then
    cp -r "MyDockerProject/wwwroot/media" "$exportDir/media"
    echo "Media files exported"
else
    echo "Warning: Media directory not found at MyDockerProject/wwwroot/media"
fi

# Step 4: Export views
echo ""
echo "Exporting views..."
if [ -d "MyDockerProject/Views" ]; then
    cp -r "MyDockerProject/Views" "$exportDir/Views"
    echo "Views exported"
else
    echo "Warning: Views directory not found at MyDockerProject/Views"
fi

# Step 5: Create manifest
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

# Step 6: Create compressed archive (optional)
echo ""
echo "Creating compressed archive..."
if command -v tar &> /dev/null; then
    tar -czf "$exportDir.tar.gz" -C "$(dirname $exportDir)" "$(basename $exportDir)"
    echo "Compressed archive created: $exportDir.tar.gz"
else
    echo "Warning: tar not found, skipping compression"
fi

echo ""
echo "=== Export Complete ==="
echo "Export location: $exportDir"
echo "Files:"
echo "  - umbracoDb.bak (database backup)"
echo "  - media/ (media files)"
echo "  - Views/ (custom views)"
echo "  - manifest.json (export info)"
if [ -f "$exportDir.tar.gz" ]; then
    echo "  - $exportDir.tar.gz (compressed archive)"
fi

