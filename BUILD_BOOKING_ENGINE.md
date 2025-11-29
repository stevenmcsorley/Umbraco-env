# Building the React Booking Engine

The React booking engine is **automatically built as part of the Docker build process**. The Dockerfile includes a multi-stage build that:

1. Builds the React app from `frontend/` folder
2. Copies the built `booking-engine.js` to `wwwroot/scripts/`
3. Includes it in the final Docker image

## Docker Build (Automatic)

The React booking engine is built automatically when you run:
```bash
docker-compose build mydockerproject
```

Or when rebuilding:
```bash
docker-compose up --build
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

# Copy the built file to wwwroot
cp dist/booking-engine.iife.js MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js
```

**Note**: Local builds are only needed for development. In Docker, the build happens automatically.

## File Locations

- **Source**: `frontend/src/booking-engine/`
- **Entry Point**: `frontend/src/booking-engine-entry.tsx`
- **Built Output**: `frontend/dist/booking-engine.js`
- **Deployment Location**: `MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js`

## Development

For development, you can run the Vite dev server:
```bash
cd frontend
npm run dev
```

Then update the CSHTML files to use `http://localhost:5173/booking-engine.js` during development.

For production/Docker, use `/scripts/booking-engine.js` (which is what the CSHTML files now use).

