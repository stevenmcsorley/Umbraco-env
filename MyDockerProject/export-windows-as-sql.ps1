# PowerShell script to export Umbraco database from Windows as SQL scripts
# Run this on Windows machine to create Pi5-compatible export

param(
    [string]$Server = "localhost",
    [string]$Database = "umbracoDb",
    [string]$OutputDir = ".\umbraco-export-sql-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
)

Write-Host "=== Umbraco SQL Script Export for Pi5 ==="
Write-Host "Server: $Server"
Write-Host "Database: $Database"
Write-Host "Output: $OutputDir"
Write-Host ""

# Create output directory
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Export schema
Write-Host "Exporting schema..."
$schemaFile = Join-Path $OutputDir "schema.sql"
sqlcmd -S $Server -d $Database -E -Q "EXEC sp_helpdb '$Database'" -o $schemaFile 2>&1 | Out-Null

# Get list of tables
Write-Host "Getting table list..."
$tables = sqlcmd -S $Server -d $Database -E -h -1 -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME" | Where-Object { $_.Trim() -ne "" }

Write-Host "Found $($tables.Count) tables"
Write-Host ""

# Export each table
foreach ($table in $tables) {
    $table = $table.Trim()
    if ($table) {
        Write-Host "Exporting table: $table"
        $tableFile = Join-Path $OutputDir "$table.sql"
        
        # Export as INSERT statements
        $query = @"
SET NOCOUNT ON;
SELECT 'INSERT INTO [$table] (' + 
    STUFF((
        SELECT ', [' + COLUMN_NAME + ']'
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = '$table'
        ORDER BY ORDINAL_POSITION
        FOR XML PATH('')
    ), 1, 2, '') + ') VALUES (' +
    STUFF((
        SELECT ', ' + 
            CASE 
                WHEN DATA_TYPE IN ('varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext') 
                    THEN '''' + REPLACE(CAST([' + COLUMN_NAME + '] AS VARCHAR(MAX)), '''', '''''') + ''''
                WHEN DATA_TYPE IN ('datetime', 'datetime2', 'date', 'time') 
                    THEN '''' + CONVERT(VARCHAR, [' + COLUMN_NAME + '], 120) + ''''
                ELSE 'CAST([' + COLUMN_NAME + '] AS VARCHAR(MAX))'
            END
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = '$table'
        ORDER BY ORDINAL_POSITION
        FOR XML PATH('')
    ), 1, 2, '') + ');'
FROM [$table]
"@
        
        sqlcmd -S $Server -d $Database -E -h -1 -Q $query -o $tableFile 2>&1 | Out-Null
    }
}

# Copy media and views if they exist
$mediaSource = "MyDockerProject\wwwroot\media"
$viewsSource = "MyDockerProject\Views"

if (Test-Path $mediaSource) {
    Write-Host "Copying media files..."
    Copy-Item -Path $mediaSource -Destination (Join-Path $OutputDir "media") -Recurse -Force
}

if (Test-Path $viewsSource) {
    Write-Host "Copying views..."
    Copy-Item -Path $viewsSource -Destination (Join-Path $OutputDir "Views") -Recurse -Force
}

# Create manifest
$manifest = @{
    exportDate = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    exportType = "sql-scripts"
    database = $Database
    server = $Server
    version = "1.0"
} | ConvertTo-Json

$manifest | Out-File (Join-Path $OutputDir "manifest.json") -Encoding UTF8

Write-Host ""
Write-Host "=== Export Complete ==="
Write-Host "Export location: $OutputDir"
Write-Host ""
Write-Host "To import on Pi5:"
Write-Host "  ./import-sql-scripts.sh $OutputDir [db-password]"

