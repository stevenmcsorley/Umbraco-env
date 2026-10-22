# Getting Started Guide

## Quick Start

### 1. Build React Booking Engine

```bash
cd frontend
npm ci
npm run build
# Copy built assets (only needed for a local non-Docker host)
mkdir -p ../MyDockerProject/MyDockerProject/wwwroot/scripts ../MyDockerProject/MyDockerProject/wwwroot/css
cp dist/booking-engine.iife.js ../MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js
cp dist/booking-engine.css ../MyDockerProject/MyDockerProject/wwwroot/css/booking-engine.css
```

Or use the build script:
```bash
cd MyDockerProject
chmod +x build-booking-engine.sh
./build-booking-engine.sh
```

### 2. Start Docker Services

```bash
cd MyDockerProject
docker compose up -d --build
```

This starts:
- SQL Server database (port 1433)
- Umbraco CMS (port 44372)
- Booking Engine Backend (port 3001)

The Umbraco Dockerfile builds the React booking bundle. The base Compose host
uses HTTP on port 44372 and persists ASP.NET Core data-protection keys in the
existing Umbraco data volume. OpenIddict requires authorization requests over
HTTPS, so use the HTTPS overlay for backoffice sign-in. The login page is
reachable; complete backoffice credential sign-in has not yet been verified
for every local setup. Production hosting must also provide key encryption.
Session storage is in-memory, so container restarts log active sessions out
even though the data-protection key ring stays intact.

For local sign-in, create and trust a development certificate once, then start
the optional HTTPS Compose overlay. If `MyDockerProject/https/sitekit-dev.pfx`
already exists, use the password that was set when it was created instead of
replacing the certificate:

```powershell
Set-Location MyDockerProject
New-Item -ItemType Directory -Force https | Out-Null
$env:SITEKIT_HTTPS_CERT_PASSWORD = 'choose-a-local-password'
dotnet dev-certs https -ep https\sitekit-dev.pfx -p $env:SITEKIT_HTTPS_CERT_PASSWORD
dotnet dev-certs https --trust
docker compose -f docker-compose.yml -f docker-compose.https.yml up -d --build
```

If starting from the repository root with an existing certificate, set
`SITEKIT_HTTPS_CERT_PASSWORD` to that certificate's password before running
Compose:

```powershell
$env:SITEKIT_HTTPS_CERT_PASSWORD = '<the certificate password>'
docker compose -f MyDockerProject/docker-compose.yml -f MyDockerProject/docker-compose.https.yml up -d --build
```

Open `https://localhost:44373/umbraco` directly. The HTTP port 44372 redirects
to HTTPS, but starting at the HTTPS URL avoids an HTTP authorization request.
Do not use the backoffice URL on port 44372: if the HTTPS overlay is not active,
that listener has no TLS and OpenIddict rejects authorization requests with
`ID2083` (`This server only accepts HTTPS requests`). Confirm that port 44373 is
published before signing in. For a reverse proxy, forward `X-Forwarded-Proto:
https` and configure the proxy's address under `ForwardedHeaders:KnownProxies`;
the app intentionally does not trust arbitrary forwarded headers. Stop the
services with the same Compose files and `down`. The certificate directory is
ignored by Git.

For anonymous guest booking management links in confirmation emails, configure
`SITEKIT_PUBLIC_BASE_URL` in the private `.env` file (for local HTTPS, use
`https://localhost:44373`). A confirmation email carries a random bearer
credential in the URL fragment; the browser sends it in an Authorization
header, and only its SHA-256 hash is stored with the booking. Existing bookings
remain available to administrators but cannot be self-managed through the new
guest endpoint because a credential cannot be safely reconstructed for them.

### 3. Access Services

- **Umbraco Backoffice (sign-in)**: https://localhost:44373/umbraco (HTTPS overlay required)
- **Umbraco HTTP endpoint**: http://localhost:44372/umbraco (sign-in is rejected over HTTP)
- **Umbraco API**: http://localhost:44372/api/hotels
- **Booking Engine API**: http://localhost:3001/health

### 4. Create Content Types

Use the Umbraco Site Kit API to create content types programmatically:

```powershell
# Create Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-partial-element-types" -Method POST

# Add properties to Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/add-properties-to-element-types" -Method POST

# Create Document Types (Hotel, Room, Offer)
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-document-types" -Method POST

# Add properties to Hotel
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/add-hotel-properties" -Method POST
```

### 5. Create Demo Content

```bash
curl -X POST http://localhost:44372/api/seed/create-demo-hotel
```

### 6. View Pages

- **Hotel List**: http://localhost:44372/hotels
- **Hotel Details**: http://localhost:44372/hotels/{slug}
- **Room Page**: http://localhost:44372/hotels/{hotelSlug}/rooms/{roomSlug}

## Development

### Rebuilding React Booking Engine

After making changes to the React booking engine:

```bash
cd frontend
npm run build
cp dist/booking-engine.iife.js ../MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js
```

### Rebuilding Docker Services

```bash
cd MyDockerProject
docker compose up -d --build
```

### Viewing Logs

```bash
docker compose logs -f mydockerproject
docker compose logs -f booking_engine
```

## Project Structure

```
├── MyDockerProject/              # Umbraco CMS project (Docker)
│   ├── engine/                    # Booking Engine Backend (Node.js)
│   ├── MyDockerProject/
│   │   ├── Views/                 # Razor views and partials
│   │   ├── Controllers/           # API and page controllers
│   │   ├── Services/              # Business logic
│   │   └── wwwroot/scripts/       # Built React booking engine
│   └── docker-compose.yml
├── frontend/                       # React Booking Engine source (build separately)
│   └── src/booking-engine/
└── docs/                          # Documentation
```

## Documentation

- **[README.md](../README.md)** - Main project documentation
- **[Umbraco Site Kit Guide](UMBRACO_SITE_KIT.md)** ⭐ - Complete guide to programmatic Document Type and Element Type creation
- **[Architecture](architecture.md)** - System architecture
- **[Booking Engine Implementation](../BOOKING_ENGINE_IMPLEMENTATION.md)** - Booking engine details
- **[Build Booking Engine](../BUILD_BOOKING_ENGINE.md)** - React build instructions

