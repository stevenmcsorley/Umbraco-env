#!/bin/bash
set -e

# Import CSV data using BCP (Bulk Copy Program)
# This imports the exported CSV files into Pi5 database

if [ -z "$1" ]; then
    echo "Usage: $0 <path-to-export-directory> [db-password]"
    echo "Example: $0 ../umbraco-full-export-20251202-053000 Password1234"
    exit 1
fi

EXPORT_DIR="$1"
if [ ! -d "$EXPORT_DIR" ]; then
    echo "Error: Export directory not found: $EXPORT_DIR"
    exit 1
fi

if [ -f .env ]; then
    source .env
    DB_PASSWORD="${2:-${DB_PASSWORD:-Password1234}}"
else
    DB_PASSWORD="${2:-Password1234}"
fi

echo "=== Importing CSV Data to Pi5 ==="
echo "Source: $EXPORT_DIR"
echo ""

# Find BCP
BCP_PATH=""
for path in "/opt/mssql-tools18/bin/bcp" "/opt/mssql-tools/bin/bcp" "bcp"; do
    if docker exec mydockerproject_database test -f "$path" 2>/dev/null || docker exec mydockerproject_database command -v "$path" >/dev/null 2>&1; then
        BCP_PATH="$path"
        break
    fi
done

if [ -z "$BCP_PATH" ]; then
    echo "Error: BCP not found. Cannot import CSV files."
    echo "You'll need to convert CSV to INSERT statements instead."
    exit 1
fi

echo "Using BCP at: $BCP_PATH"
echo ""

# Stop Umbraco
echo "Stopping Umbraco..."
docker compose stop mydockerproject 2>/dev/null || true

# Import tables in dependency order
TABLES=(
    "umbracoNode"
    "umbracoContent"
    "umbracoContentVersion"
    "umbracoDocument"
    "umbracoDocumentVersion"
    "umbracoContentType"
    "umbracoDataType"
    "umbracoPropertyType"
    "umbracoPropertyData"
    "umbracoLanguage"
    "umbracoDomain"
)

DATA_DIR="$EXPORT_DIR/data"

for table in "${TABLES[@]}"; do
    CSV_FILE="$DATA_DIR/${table}.csv"
    if [ -f "$CSV_FILE" ] && [ -s "$CSV_FILE" ]; then
        echo "Importing $table..."
        
        # Copy CSV to container
        docker cp "$CSV_FILE" mydockerproject_database:/tmp/${table}.csv
        
        # Use BCP to import
        docker exec mydockerproject_database $BCP_PATH \
            "umbracoDb.dbo.$table" in "/tmp/${table}.csv" \
            -S localhost -U sa -P "$DB_PASSWORD" \
            -c -t "|" -F 2 2>&1 | head -3 || echo "  Warning: Import may have had errors"
    else
        echo "Skipping $table (file not found or empty)"
    fi
done

# Copy media and views
if [ -d "$EXPORT_DIR/media" ]; then
    echo ""
    echo "Copying media files..."
    mkdir -p "MyDockerProject/wwwroot/media"
    cp -r "$EXPORT_DIR/media/"* "MyDockerProject/wwwroot/media/" 2>/dev/null || true
fi

if [ -d "$EXPORT_DIR/Views" ]; then
    echo "Copying views..."
    mkdir -p "MyDockerProject/Views"
    cp -r "$EXPORT_DIR/Views/"* "MyDockerProject/Views/" 2>/dev/null || true
fi

# Restart
echo ""
echo "Restarting services..."
docker compose up -d mydockerproject 2>/dev/null || docker compose restart

echo ""
echo "=== Import Complete ==="
echo ""
echo "Check the database:"
echo "  docker exec mydockerproject_database /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $DB_PASSWORD -d umbracoDb -C -h -1 -Q \"SELECT COUNT(*) FROM umbracoContent\""

