# Setup Events and Offers for Hotels
# IMPORTANT: Restart your Umbraco application BEFORE running this script!
# 
# To restart:
#   - If running in terminal: Press Ctrl+C, then run `dotnet run` again
#   - If running in Visual Studio: Stop (Shift+F5) and Start (F5) again
#   - If running via Docker: `docker-compose restart`

Write-Host "`n=== Setting Up Events and Offers ===" -ForegroundColor Cyan
Write-Host "Make sure you've restarted the Umbraco application first!" -ForegroundColor Yellow
Write-Host "Press any key to continue or Ctrl+C to cancel..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Step 1: Add Event Properties
Write-Host "`n[1/3] Adding properties to Event document type..." -ForegroundColor Yellow
try {
    $response1 = Invoke-WebRequest -Uri "http://localhost:44372/api/seed/add-event-properties" -Method POST -ContentType "application/json" -ErrorAction Stop
    $result1 = $response1.Content | ConvertFrom-Json
    Write-Host "✓ Event properties added successfully!" -ForegroundColor Green
    Write-Host "  $($result1.message)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to add event properties: $_" -ForegroundColor Red
    Write-Host "  Make sure the application has been restarted." -ForegroundColor Yellow
    exit 1
}

# Step 2: Seed Events and Offers for all Hotels
Write-Host "`n[2/3] Seeding events and offers for all hotels..." -ForegroundColor Yellow
try {
    $response2 = Invoke-WebRequest -Uri "http://localhost:44372/api/seed/add-offers-events-to-hotels" -Method POST -ContentType "application/json" -ErrorAction Stop
    $result2 = $response2.Content | ConvertFrom-Json
    Write-Host "✓ Events and offers seeded successfully!" -ForegroundColor Green
    Write-Host "  $($result2.message)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to seed events/offers: $_" -ForegroundColor Red
    Write-Host "  Make sure the application has been restarted." -ForegroundColor Yellow
    exit 1
}

# Step 3: Verify Results
Write-Host "`n[3/3] Verifying results..." -ForegroundColor Yellow
try {
    $hotelsResponse = Invoke-WebRequest -Uri "http://localhost:44372/api/hotels" -Method GET -ErrorAction Stop
    $hotels = $hotelsResponse.Content | ConvertFrom-Json
    
    Write-Host "✓ Found $($hotels.Count) hotels" -ForegroundColor Green
    
    foreach ($hotel in $hotels) {
        Write-Host "`n  Hotel: $($hotel.name)" -ForegroundColor White
        
        # Check offers
        try {
            $offersResponse = Invoke-WebRequest -Uri "http://localhost:44372/api/hotels/$($hotel.slug)/offers" -Method GET -ErrorAction Stop
            $offers = $offersResponse.Content | ConvertFrom-Json
            Write-Host "    Offers: $($offers.Count)" -ForegroundColor Gray
        } catch {
            Write-Host "    Offers: 0 (or error fetching)" -ForegroundColor Gray
        }
        
        # Check events
        try {
            $eventsResponse = Invoke-WebRequest -Uri "http://localhost:44372/api/hotels/$($hotel.slug)/events" -Method GET -ErrorAction Stop
            $events = $eventsResponse.Content | ConvertFrom-Json
            Write-Host "    Events: $($events.Count)" -ForegroundColor Gray
        } catch {
            Write-Host "    Events: 0 (or error fetching)" -ForegroundColor Gray
        }
    }
    
    Write-Host "`n✓ Setup complete!" -ForegroundColor Green
    Write-Host "`nYou can now:" -ForegroundColor Cyan
    Write-Host "  - View events/offers in Umbraco backoffice (Content → Hotel → Events/Offers)" -ForegroundColor Gray
    Write-Host "  - View them on hotel pages: http://localhost:44372/hotels/{hotel-slug}" -ForegroundColor Gray
    
} catch {
    Write-Host "✗ Failed to verify: $_" -ForegroundColor Red
}

