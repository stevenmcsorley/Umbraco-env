#!/bin/bash
# Build script for React Booking Engine
# This builds the booking engine from the root frontend folder and copies it to wwwroot/scripts

echo "Building React Booking Engine..."

# Navigate to frontend directory (from MyDockerProject root, go up one level)
cd ../frontend

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

# Build the booking engine
echo "Building booking engine..."
npm run build

# Copy the built file to wwwroot/scripts
echo "Copying built file to wwwroot/scripts..."
mkdir -p MyDockerProject/MyDockerProject/wwwroot/scripts
cp dist/booking-engine.js MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js

echo "Booking engine built and copied successfully!"

