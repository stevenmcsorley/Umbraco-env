# Setup Guide for Pi5 Server

Quick setup guide to import and run the Umbraco app on your Pi5 server.

## Prerequisites

- Docker and Docker Compose installed
- Git installed
- Port 44372 available (or change in docker-compose.yml)

## Quick Setup (One-Time)

```bash
# 1. Navigate to apps directory
cd /home/dev/apps

# 2. Clone repository (if not already cloned)
git clone https://github.com/stevenmcsorley/Umbraco-env.git
cd Umbraco-env/MyDockerProject

# 3. Make import script executable
chmod +x import-umbraco.sh

# 4. Start services (database needs to be running first)
docker compose up -d

# 5. Wait for database to initialize (30-60 seconds)
echo "Waiting for database to be ready..."
sleep 40

# 6. Import Windows export (restores database, media, views)
./import-umbraco.sh "../umbraco-export-20251201-225510" "Password1234"

# 7. Verify it's working
curl -s http://localhost:44372/ | head -20
```

## What the Import Does

The import script automatically:
- ✅ Stops Umbraco container
- ✅ Restores database from Windows export (`umbracoDb.bak`)
- ✅ Copies all media files to `wwwroot/media`
- ✅ Restores all views/templates
- ✅ Sets correct file permissions
- ✅ Restarts all services

## Access the App

- **Umbraco Backoffice**: http://localhost:44372/umbraco
- **Homepage**: http://localhost:44372/
- **API**: http://localhost:44372/api/hotels

## Database Password

The default password is `Password1234`. If you changed it, update the import command:
```bash
./import-umbraco.sh "../umbraco-export-20251201-225510" "YourActualPassword"
```

## Troubleshooting

### Database restore fails
```bash
# Stop Umbraco first
docker compose stop mydockerproject

# Then run import again
./import-umbraco.sh "../umbraco-export-20251201-225510" "Password1234"
```

### Services won't start
```bash
# Check logs
docker compose logs

# Restart services
docker compose restart
```

### Port already in use
Edit `docker-compose.yml` and change port mapping:
```yaml
ports:
  - "8080:8080"  # Change 44372 to your preferred port
```

## Update from Repository

To pull latest changes:
```bash
cd /home/dev/apps/Umbraco-env
git pull origin main
cd MyDockerProject
docker compose up -d --build
```

