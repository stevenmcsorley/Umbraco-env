# Restarting Booking Engine After Updates

## What Changed

1. **Booking Engine Backend**: Added `UmbracoAdapter` to fetch real data from Umbraco API
2. **Booking Service**: Updated to handle add-ons pricing
3. **React Booking Engine**: Built and copied to `wwwroot/scripts/booking-engine.js`
4. **Docker Compose**: Removed frontend service (React is now served from Umbraco)

## Steps to Restart

### 1. Stop Old Frontend Service (if still running)

```bash
cd MyDockerProject
docker-compose stop frontend
docker-compose rm -f frontend
```

### 2. Rebuild and Restart Booking Engine

Since we added new code (UmbracoAdapter), the booking engine needs to be rebuilt:

```bash
cd MyDockerProject
docker-compose up -d --build booking_engine
```

### 3. Verify Services

```bash
# Check booking engine health
curl http://localhost:3001/health

# Check services are running
docker-compose ps
```

### 4. Test Booking Engine

1. Navigate to a room page: http://localhost:44372/hotels/{hotelSlug}/rooms/{roomSlug}
2. The booking engine should load from `/scripts/booking-engine.js`
3. Check browser console for any errors

## Quick Restart (All Services)

If you want to restart everything:

```bash
cd MyDockerProject
docker-compose down
docker-compose up -d --build
```

## Troubleshooting

### Booking Engine Not Loading
- Check browser console for 404 errors on `/scripts/booking-engine.js`
- Verify file exists: `MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js`
- Check Umbraco is serving static files correctly

### Booking Engine API Errors
- Check booking engine logs: `docker-compose logs booking_engine`
- Verify Umbraco API is accessible: `curl http://localhost:44372/api/hotels`
- Check environment variables in docker-compose.yml

### Umbraco Adapter Not Working
- Verify `UMBRACO_API_BASE` environment variable is set correctly
- Check network connectivity between containers
- Review booking engine logs for API errors

