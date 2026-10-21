#!/bin/bash
# Build script for React Booking Engine
# This builds the booking engine from the root frontend folder and copies it to wwwroot/scripts

echo "Building React Booking Engine..."

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root/frontend"

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
mkdir -p "$repo_root/MyDockerProject/MyDockerProject/wwwroot/scripts" "$repo_root/MyDockerProject/MyDockerProject/wwwroot/css"
cp dist/booking-engine.iife.js "$repo_root/MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js"
cp dist/booking-engine.css "$repo_root/MyDockerProject/MyDockerProject/wwwroot/css/booking-engine.css"

echo "Booking engine built and copied successfully!"

