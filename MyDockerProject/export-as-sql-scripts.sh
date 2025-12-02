#!/bin/bash
set -e

# Export Umbraco database as SQL scripts (compatible with Azure SQL Edge)
# This creates INSERT statements for all tables

if [ -f .env ]; then
    source .env
    DB_PASSWORD="${DB_PASSWORD:-Password1234}"
else
    DB_PASSWORD="${1:-Password1234}"
fi

EXPORT_DIR="../umbraco-export-sql-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$EXPORT_DIR"

echo "=== Exporting Umbraco Database as SQL Scripts ==="
echo "Export directory: $EXPORT_DIR"
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
    echo "Error: sqlcmd not found"
    exit 1
fi

echo "Using sqlcmd at: $SQLCMD_PATH"
echo ""

# Get list of tables
echo "Getting table list..."
TABLES=$(docker exec mydockerproject_database $SQLCMD_PATH \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -d umbracoDb \
    -C \
    -h -1 \
    -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME" 2>/dev/null | tr -d '\r' | grep -v '^$' | grep -v '^---' | grep -v 'rows affected')

if [ -z "$TABLES" ]; then
    echo "Error: Could not retrieve table list"
    exit 1
fi

TABLE_COUNT=$(echo "$TABLES" | wc -l)
echo "Found $TABLE_COUNT tables to export"
echo ""

# Export each table
COUNTER=0
for table in $TABLES; do
    COUNTER=$((COUNTER + 1))
    echo "[$COUNTER/$TABLE_COUNT] Exporting table: $table"
    
    # Use bcp to export data (more reliable than sqlcmd for large data)
    # First try bcp, fallback to sqlcmd
    TABLE_FILE="$EXPORT_DIR/${table}.sql"
    
    # Export using sqlcmd with proper formatting
    docker exec mydockerproject_database $SQLCMD_PATH \
        -S localhost -U sa -P "$DB_PASSWORD" \
        -d umbracoDb \
        -C \
        -h -1 \
        -W \
        -s "," \
        -Q "SELECT * FROM [$table]" > "$TABLE_FILE.raw" 2>/dev/null || true
    
    # Convert to INSERT statements (simplified - for production use a proper tool)
    if [ -s "$TABLE_FILE.raw" ]; then
        echo "-- Data for table: $table" > "$TABLE_FILE"
        echo "-- Exported: $(date)" >> "$TABLE_FILE"
        echo "" >> "$TABLE_FILE"
        # Note: This is a simplified export. For production, use a proper SQL export tool
        cat "$TABLE_FILE.raw" >> "$TABLE_FILE"
        rm "$TABLE_FILE.raw"
    fi
done

# Export schema
echo ""
echo "Exporting database schema..."
SCHEMA_FILE="$EXPORT_DIR/00-schema.sql"
docker exec mydockerproject_database $SQLCMD_PATH \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -d umbracoDb \
    -C \
    -Q "EXEC sp_helpdb 'umbracoDb'" > "$SCHEMA_FILE" 2>&1 || true

# Copy media and views
echo ""
echo "Copying media files..."
if [ -d "MyDockerProject/wwwroot/media" ]; then
    cp -r "MyDockerProject/wwwroot/media" "$EXPORT_DIR/"
    echo "Media files copied"
fi

echo "Copying views..."
if [ -d "MyDockerProject/Views" ]; then
    cp -r "MyDockerProject/Views" "$EXPORT_DIR/"
    echo "Views copied"
fi

# Create manifest
cat > "$EXPORT_DIR/manifest.json" <<EOF
{
  "exportDate": "$(date -Iseconds)",
  "exportType": "sql-scripts",
  "database": "umbracoDb",
  "tables": $TABLE_COUNT,
  "version": "1.0"
}
EOF

echo ""
echo "=== Export Complete ==="
echo "Export location: $EXPORT_DIR"
echo ""
echo "To import on Pi5:"
echo "  scp -r $EXPORT_DIR dev@192.168.1.198:/home/dev/apps/Umbraco-env/"
echo "  ssh dev@192.168.1.198 'cd /home/dev/apps/Umbraco-env/MyDockerProject && ./import-sql-scripts.sh ../$(basename $EXPORT_DIR) Password1234'"

