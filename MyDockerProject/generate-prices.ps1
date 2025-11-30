# Generate prices for all hotels and rooms
$hotels = Invoke-RestMethod -Uri "http://localhost:44372/api/hotels" -Method GET
$importData = @{
    hotels = @()
}

$baseDate = Get-Date
$daysToGenerate = 90

# Price ranges for different room types
$roomPriceRanges = @{
    "Presidential Suite" = @{ min = 600; max = 1000 }
    "Royal Suite" = @{ min = 500; max = 800 }
    "Executive Suite" = @{ min = 400; max = 700 }
    "Deluxe Double Room" = @{ min = 150; max = 300 }
    "Premium Single Room" = @{ min = 100; max = 200 }
    "Family Suite" = @{ min = 300; max = 500 }
    "Ocean View Suite" = @{ min = 250; max = 450 }
    "Penthouse with Terrace" = @{ min = 500; max = 900 }
    "Standard Double Room" = @{ min = 120; max = 250 }
    "Mountain View Chalet" = @{ min = 300; max = 500 }
    "Ski Suite" = @{ min = 400; max = 600 }
    "Overwater Villa" = @{ min = 600; max = 1000 }
    "Beachfront Suite" = @{ min = 400; max = 700 }
    "Canal View Room" = @{ min = 200; max = 350 }
    "Designer Suite" = @{ min = 350; max = 550 }
    "Heritage Double Room" = @{ min = 180; max = 320 }
    "Family Room" = @{ min = 250; max = 400 }
    "Vineyard View Suite" = @{ min = 400; max = 650 }
    "Tuscan Farmhouse Room" = @{ min = 200; max = 350 }
    "Ski-In/Ski-Out Penthouse" = @{ min = 700; max = 1100 }
    "Mountain View Suite" = @{ min = 350; max = 550 }
    "Standard Ski Room" = @{ min = 250; max = 400 }
    "Royal Riad Suite" = @{ min = 450; max = 750 }
    "Traditional Courtyard Room" = @{ min = 180; max = 300 }
}

Write-Host "Processing $($hotels.Count) hotels..." -ForegroundColor Cyan

foreach ($hotel in $hotels) {
    $hotelData = @{
        name = $hotel.name
        rooms = @()
    }
    
    Write-Host "  Hotel: $($hotel.name)" -ForegroundColor Yellow
    
    try {
        $rooms = Invoke-RestMethod -Uri "http://localhost:44372/api/hotels/$($hotel.id)/rooms" -Method GET -ErrorAction SilentlyContinue
        
        if ($rooms) {
            foreach ($room in $rooms) {
                $prices = @{}
                $availability = @{}
                
                # Get price range for this room type
                $priceRange = $roomPriceRanges[$room.name]
                if (-not $priceRange) {
                    $priceRange = @{ min = 150; max = 300 }
                }
                
                Write-Host "    Room: $($room.name)" -ForegroundColor Gray
                
                # Generate prices for next 90 days
                for ($i = 0; $i -lt $daysToGenerate; $i++) {
                    $date = $baseDate.AddDays($i)
                    $dateStr = $date.ToString("yyyy-MM-dd")
                    $dayOfWeek = $date.DayOfWeek
                    $isWeekend = $dayOfWeek -eq "Saturday" -or $dayOfWeek -eq "Sunday"
                    $isHoliday = ($date.Month -eq 12 -and $date.Day -ge 20 -and $date.Day -le 31) -or 
                                 ($date.Month -eq 1 -and $date.Day -le 5)
                    
                    # Base price with random variation
                    $basePrice = Get-Random -Minimum $priceRange.min -Maximum $priceRange.max
                    
                    # Weekend surcharge
                    if ($isWeekend) {
                        $basePrice = [math]::Round($basePrice * 1.2, 2)
                    }
                    
                    # Holiday surcharge
                    if ($isHoliday) {
                        $basePrice = [math]::Round($basePrice * 1.3, 2)
                    }
                    
                    $prices[$dateStr] = [math]::Round($basePrice, 2)
                    $availability[$dateStr] = Get-Random -Minimum 2 -Maximum 8
                }
                
                $hotelData.rooms += @{
                    name = $room.name
                    prices = $prices
                    availability = $availability
                }
            }
        }
    }
    catch {
        Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    if ($hotelData.rooms.Count -gt 0) {
        $importData.hotels += $hotelData
    }
}

# Save to file
$json = $importData | ConvertTo-Json -Depth 10
$json | Out-File -FilePath "import-all-prices.json" -Encoding UTF8

Write-Host "`n✅ Generated import file:" -ForegroundColor Green
Write-Host "   Hotels: $($importData.hotels.Count)" -ForegroundColor Cyan
$totalRooms = ($importData.hotels | ForEach-Object { $_.rooms.Count } | Measure-Object -Sum).Sum
Write-Host "   Total rooms: $totalRooms" -ForegroundColor Cyan
Write-Host "   Days per room: $daysToGenerate" -ForegroundColor Cyan
Write-Host "   File: import-all-prices.json`n" -ForegroundColor Cyan

