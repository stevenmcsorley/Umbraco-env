# Quick Export/Import Guide

Simple scripts to export from one server and import to another.

## Export from Source Server (Linux)

```bash
cd /path/to/Umbraco-env/MyDockerProject
./export-umbraco.sh
```

This will:
- Create a timestamped export directory: `../umbraco-export-YYYYMMDD-HHMMSS/`
- Export database backup (`umbracoDb.bak`)
- Export media files (`media/`)
- Export views (`Views/`)
- Optionally create a compressed `.tar.gz` archive

**Example output:**
```
=== Umbraco Export Script ===
Export directory: ../umbraco-export-20251202-030000
...
=== Export Complete ===
Export location: ../umbraco-export-20251202-030000
```

## Transfer to Destination Server

**Option 1: Copy directory**
```bash
scp -r ../umbraco-export-20251202-030000 user@destination:/path/to/destination/
```

**Option 2: Copy compressed archive (if created)**
```bash
scp ../umbraco-export-20251202-030000.tar.gz user@destination:/path/to/destination/
```

## Import on Destination Server (Pi5)

```bash
cd /home/dev/apps/Umbraco-env/MyDockerProject

# From directory
./import-umbraco.sh ../umbraco-export-20251202-030000

# Or from compressed archive
./import-umbraco.sh ../umbraco-export-20251202-030000.tar.gz

# With custom password
./import-umbraco.sh ../umbraco-export-20251202-030000 YourPassword
```

This will:
- Stop Umbraco container
- Restore database from backup
- Copy media files
- Restore views
- Set permissions
- Restart services

## Notes

- **Database compatibility**: Exports from SQL Server 2022 (Windows) may not restore on Azure SQL Edge (Pi5 ARM64). If restore fails, use API import endpoints instead.
- **Password**: Default is `Password1234`. Can be set in `.env` file or passed as second argument.
- **Permissions**: Scripts handle file permissions automatically on Linux.

## Troubleshooting

**Database restore fails:**
- Check database container is running: `docker compose ps`
- Verify password is correct
- For version incompatibility, use API import endpoints instead

**Media/Views not restored:**
- Check export directory contains `media/` and `Views/` folders
- Verify file permissions: `ls -la MyDockerProject/wwwroot/media`

