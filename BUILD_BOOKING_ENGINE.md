# Building the React Booking Engine

The React booking engine is **automatically built as part of the Docker build process**. The Dockerfile includes a multi-stage build that:

1. Builds the React app from `frontend/` folder
2. Copies the built `booking-engine.iife.js` to `wwwroot/scripts/booking-engine.js`
3. Copies the generated Tailwind stylesheet to `wwwroot/css/booking-engine.css`
4. Includes both assets in the final Docker image

## Docker Build (Automatic)

The React booking engine is built automatically when you run:
```bash
docker compose build mydockerproject
```

Or when rebuilding:
```bash
docker compose up -d --build
```

## Building Locally (For Development)

If you need to build locally for development/testing:

### Option 1: Use the build script
```bash
cd MyDockerProject
chmod +x build-booking-engine.sh
./build-booking-engine.sh
```

### Option 2: Manual build
```bash
# From project root
cd frontend
npm install
npm run build

# Copy the JavaScript and stylesheet to wwwroot
cp dist/booking-engine.iife.js ../MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js
cp dist/booking-engine.css ../MyDockerProject/MyDockerProject/wwwroot/css/booking-engine.css
```

**Note**: Local builds are only needed for development. In Docker, the build happens automatically.

## File Locations

- **Source**: `frontend/src/booking-engine/`
- **Entry Point**: `frontend/src/booking-engine-entry.tsx`
- **Built Output**: `frontend/dist/booking-engine.iife.js` and `frontend/dist/booking-engine.css`
- **Deployment Locations**: `MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js` and `wwwroot/css/booking-engine.css`

## Development

For development, you can run the Vite dev server:
```bash
cd frontend
npm run dev
```

Then update the CSHTML files to use `http://localhost:5173/booking-engine.js` during development.

For production/Docker, use `/scripts/booking-engine.js` (which is what the CSHTML files now use).

