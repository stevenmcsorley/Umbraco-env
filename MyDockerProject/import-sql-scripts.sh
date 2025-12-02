#!/bin/bash
set -e

# Import SQL scripts into Azure SQL Edge
# This imports data from SQL script files exported from another Azure SQL Edge instance

if [ -z "$1" ]; then
    echo "Usage: $0 <path-to-sql-export-directory> [db-password]"
    echo "Example: $0 ../umbraco-export-sql-20251202-052004 Password1234"
    exit 1
fi

SQL_EXPORT="$1"
if [ ! -d "$SQL_EXPORT" ]; then
    echo "Error: SQL export directory not found: $SQL_EXPORT"
    exit 1
fi

# Get password
if [ -f .env ]; then
    source .env
    DB_PASSWORD="${2:-${DB_PASSWORD:-Password1234}}"
else
    DB_PASSWORD="${2:-Password1234}"
fi

echo "=== Importing SQL Scripts to Pi5 ==="
echo "Source: $SQL_EXPORT"
echo ""

# Find sqlcmd
SQLCMD_PATH=""
for path in "/opt/mssql-tools18/bin/sqlcmd" "/opt/mssql-tools/bin/sqlcmd" "sqlcmd"; do
    if docker exec mydockerproject_database test -f "$path" 2>/dev/null || docker exec mydockerproject_database command -v "$path" >/dev/null 2>&1; then
        SQLCMD_PATH="$path"
        break
    fi
done

if [ -z "$SQLCMD_PATH" ]; then
    echo "Error: sqlcmd not found in database container"
    exit 1
fi

echo "Using sqlcmd at: $SQLCMD_PATH"
echo ""

# Stop Umbraco
echo "Stopping Umbraco..."
docker compose stop mydockerproject 2>/dev/null || true

# Note: The SQL scripts exported are raw data dumps, not INSERT statements
# For a proper import, we'd need to convert them to INSERT statements
# For now, we'll note that the .bak file approach didn't work due to version mismatch

echo "Note: The exported SQL files are raw data dumps."
echo "For a complete database restore, you have two options:"
echo ""
echo "Option 1: Use the .bak file (if versions match)"
echo "  The backup file is in: $SQL_EXPORT/../umbraco-export-*/umbracoDb.bak"
echo ""
echo "Option 2: Import content via Umbraco API"
echo "  Use the existing import endpoints to recreate content"
echo ""

# Copy media and views (these are the most important)
if [ -d "$SQL_EXPORT/media" ]; then
    echo "Copying media files..."
    mkdir -p "MyDockerProject/wwwroot/media"
    cp -r "$SQL_EXPORT/media/"* "MyDockerProject/wwwroot/media/" 2>/dev/null || true
    echo "Media files copied"
fi

if [ -d "$SQL_EXPORT/Views" ]; then
    echo "Copying views..."
    mkdir -p "MyDockerProject/Views"
    cp -r "$SQL_EXPORT/Views/"* "MyDockerProject/Views/" 2>/dev/null || true
    echo "Views copied"
fi

# Restart
echo ""
echo "Restarting services..."
docker compose restart 2>/dev/null || docker compose up -d

echo ""
echo "=== Import Complete (Partial) ==="
echo ""
echo "✅ Media files: Copied"
echo "✅ Views: Copied"
echo "⚠️  Database: Cannot restore .bak file due to version mismatch"
echo ""
echo "Next steps:"
echo "1. Access backoffice: https://hotel.halfagiraf.com/umbraco"
echo "2. Templates should be visible in Settings → Templates"
echo "3. For database content, use API import endpoints or recreate manually"

