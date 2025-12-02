#!/bin/bash
set -e

# Taken from: https://github.com/CarlSargunar/Umbraco-Docker-Workshop
if [ "$1" = '/opt/mssql/bin/sqlservr' ]; then
  # If this is the container's first run, initialize the application database
  if [ ! -f /tmp/app-initialized ]; then
    # Initialize the application database asynchronously in a background process. This allows a) the SQL Server process to be the main process in the container, which allows graceful shutdown and other goodies, and b) us to only start the SQL Server process once, as opposed to starting, stopping, then starting it again.
    function initialize_app_database() {
      # Wait a bit for SQL Server to start. SQL Server's process doesn't provide a clever way to check if it's up or not, and it needs to be up before we can import the application database
      sleep 15s

      # Find sqlcmd or install it if needed
      SQLCMD=""
      for path in "/opt/mssql-tools18/bin/sqlcmd" "/opt/mssql-tools/bin/sqlcmd" "sqlcmd"; do
        if command -v "$path" >/dev/null 2>&1 || [ -f "$path" ]; then
          SQLCMD="$path"
          break
        fi
      done
      
      # If sqlcmd not found, try to install it (non-blocking)
      if [ -z "$SQLCMD" ]; then
        apt-get update -qq >/dev/null 2>&1 && \
        apt-get install -y -qq curl gnupg2 >/dev/null 2>&1 && \
        curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | apt-key add - >/dev/null 2>&1 && \
        curl -fsSL https://packages.microsoft.com/config/ubuntu/20.04/prod.list > /etc/apt/sources.list.d/mssql-release.list 2>/dev/null && \
        apt-get update -qq >/dev/null 2>&1 && \
        ACCEPT_EULA=Y DEBIAN_FRONTEND=noninteractive apt-get install -y -qq mssql-tools18 >/dev/null 2>&1 && \
        SQLCMD="/opt/mssql-tools18/bin/sqlcmd" || true
      fi
      
      # Run setup script if sqlcmd is available and setup.sql exists
      if [ -n "$SQLCMD" ] && [ -f /setup.sql ]; then
        # Wait a bit more for SQL Server to be fully ready
        sleep 5s
        $SQLCMD -S localhost -U sa -P "$SA_PASSWORD" -d master -i /setup.sql -C || echo "Warning: Could not run setup.sql (database may already be initialized)"
      else
        echo "Info: Skipping setup.sql (sqlcmd not available or setup.sql not found)"
      fi

      # Note that the container has been initialized so future starts won't wipe changes to the data
      touch /tmp/app-initialized
    }
    initialize_app_database &
  fi
fi

exec "$@"
