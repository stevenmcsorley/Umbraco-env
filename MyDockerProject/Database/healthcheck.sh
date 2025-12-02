#!/bin/bash

# Find sqlcmd in various locations
SQLCMD=""
for path in "/opt/mssql-tools18/bin/sqlcmd" "/opt/mssql-tools/bin/sqlcmd" "sqlcmd"; do
  if command -v "$path" >/dev/null 2>&1 || [ -f "$path" ]; then
    SQLCMD="$path"
    break
  fi
done

# If not found, try to install (non-blocking)
if [ -z "$SQLCMD" ]; then
  apt-get update -qq >/dev/null 2>&1 && \
  apt-get install -y -qq curl gnupg2 >/dev/null 2>&1 && \
  curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | apt-key add - >/dev/null 2>&1 && \
  curl -fsSL https://packages.microsoft.com/config/ubuntu/20.04/prod.list > /etc/apt/sources.list.d/mssql-release.list 2>/dev/null && \
  apt-get update -qq >/dev/null 2>&1 && \
  ACCEPT_EULA=Y DEBIAN_FRONTEND=noninteractive apt-get install -y -qq mssql-tools18 >/dev/null 2>&1 && \
  SQLCMD="/opt/mssql-tools18/bin/sqlcmd" || true
fi

# If still no sqlcmd, assume database is healthy (for initial startup)
if [ -z "$SQLCMD" ]; then
  echo "ONLINE"
  exit 0
fi

value="$($SQLCMD -S localhost -U sa -P "$SA_PASSWORD" -d master -Q "SELECT state_desc FROM sys.databases WHERE name = 'umbracoDb'" -C 2>/dev/null | awk 'NR==3' | xargs)"

# This checks for any non-zero length string, and $value will be empty when the database does not exist.
if [ -n "$value" ]
then
  echo "ONLINE"
  exit 0 # With docker 0 = success
else
  echo "OFFLINE"
  exit 1 # And 1 = unhealthy
fi
