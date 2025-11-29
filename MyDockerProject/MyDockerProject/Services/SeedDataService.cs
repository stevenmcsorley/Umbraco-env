using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using System.Linq;

namespace MyDockerProject.Services;

public class SeedDataService
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;

    public SeedDataService(IContentService contentService, IContentTypeService contentTypeService)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
    }

    public void SeedExampleHotel()
    {
        var hotelType = _contentTypeService.Get("hotel");
        if (hotelType == null) return;

        var root = _contentService.GetRootContent().FirstOrDefault();
        if (root == null) return;

        // Define multiple hotels for a comprehensive demo
        var hotels = new[]
        {
            new {
                Name = "Grand Hotel London",
                HotelName = "Grand Hotel London",
                Description = "A luxurious 5-star hotel in the heart of London, offering world-class amenities, exceptional service, and stunning views of the Thames. Experience British elegance at its finest.",
                ShortDescription = "The ultimate address, stunningly created and curated. Beyond the captivating entrance, discover uninterrupted views of the River Thames.",
                Address = "123 Regent Street",
                City = "London",
                Country = "United Kingdom",
                Phone = "+44 20 1234 5678",
                Email = "london@grandhotel.com",
                Highlights = "Large roof terrace with panoramic views of the River Thames and South Bank\n10-seat dining room and fully equipped kitchen\nStudy\nLiving room with private balcony\nPrivate internal lift",
                Features = "24-hour concierge service\nAward-winning spa and wellness center\nMichelin-starred restaurants\nRooftop bar with city views\nBusiness center and meeting rooms\nValet parking\nComplimentary Wi-Fi",
                Amenities = "Swimming pool\nFitness center\nSpa treatments\nRoom service\nLaundry service\nAirport transfers\nPet-friendly accommodations"
            },
            new {
                Name = "Château de Paris",
                HotelName = "Château de Paris",
                Description = "An elegant boutique hotel near the Eiffel Tower, combining French sophistication with modern luxury. Indulge in gourmet dining and impeccable service in the City of Light.",
                ShortDescription = "French sophistication meets modern luxury in the heart of Paris. Experience the City of Light from our elegant boutique hotel.",
                Address = "45 Avenue des Champs-Élysées",
                City = "Paris",
                Country = "France",
                Phone = "+33 1 42 36 78 90",
                Email = "paris@chateauhotel.com",
                Highlights = "Eiffel Tower views from select rooms\nAuthentic French bistro\nWine cellar with curated selection\nRooftop terrace\nArt gallery featuring local artists",
                Features = "Concierge service\nFine dining restaurant\nWine bar\nSpa and wellness\nBusiness facilities\nValet service\nMultilingual staff",
                Amenities = "Indoor pool\nSauna and steam room\nFitness center\nLibrary\nPet-friendly\nBicycle rental\nGuided city tours"
            },
            new {
                Name = "Manhattan Skyline Hotel",
                HotelName = "Manhattan Skyline Hotel",
                Description = "A contemporary luxury hotel in Midtown Manhattan with panoramic city views, rooftop bar, and world-class spa. Experience New York City in ultimate comfort and style.",
                ShortDescription = "Contemporary luxury in the heart of Manhattan. Panoramic city views, world-class amenities, and exceptional service await.",
                Address = "789 5th Avenue",
                City = "New York",
                Country = "United States",
                Phone = "+1 212 555 1234",
                Email = "nyc@skylinehotel.com",
                Highlights = "360-degree rooftop bar\nPanoramic city views\nState-of-the-art spa\nSignature restaurant\nSky-high infinity pool\nPrivate event spaces",
                Features = "24/7 concierge\nRooftop dining\nSpa and wellness center\nBusiness center\nFitness facility\nValet parking\nComplimentary Wi-Fi",
                Amenities = "Infinity pool\nFull-service spa\nGym with personal trainers\nMultiple dining venues\nMeeting and event spaces\nPet-friendly\nElectric vehicle charging"
            },
            new {
                Name = "Tokyo Garden Resort",
                HotelName = "Tokyo Garden Resort",
                Description = "A serene oasis in bustling Tokyo, featuring traditional Japanese design elements, zen gardens, and authentic kaiseki dining. Perfect blend of modern luxury and ancient culture.",
                ShortDescription = "A serene oasis in bustling Tokyo. Traditional Japanese elegance meets modern luxury in our zen-inspired resort.",
                Address = "12-1 Shibuya Crossing",
                City = "Tokyo",
                Country = "Japan",
                Phone = "+81 3 1234 5678",
                Email = "tokyo@gardenresort.com",
                Highlights = "Traditional zen gardens\nAuthentic kaiseki restaurant\nOnsen hot spring baths\nTea ceremony room\nTraditional tatami suites\nRooftop meditation space",
                Features = "Traditional Japanese hospitality\nKaiseki dining experience\nOnsen facilities\nTea ceremony\nCultural activities\nConcierge service\nMultilingual staff",
                Amenities = "Onsen hot springs\nZen gardens\nTraditional spa\nFitness center\nMultiple restaurants\nMeeting rooms\nCultural experiences"
            },
            new {
                Name = "Dubai Marina Luxury",
                HotelName = "Dubai Marina Luxury",
                Description = "Ultra-modern beachfront hotel with infinity pools, private beach access, and Michelin-starred restaurants. Experience the pinnacle of Middle Eastern luxury and hospitality.",
                ShortDescription = "Ultra-modern beachfront luxury. Infinity pools, private beach, and Michelin-starred dining in the heart of Dubai Marina.",
                Address = "Marina Walk, Dubai Marina",
                City = "Dubai",
                Country = "United Arab Emirates",
                Phone = "+971 4 567 8901",
                Email = "dubai@marinaluxury.com",
                Highlights = "Private beach access\nMultiple infinity pools\nMichelin-starred restaurants\nBeachfront cabanas\nYacht marina access\nRooftop sky bar\nHelipad",
                Features = "Beachfront location\nMultiple pools\nFine dining restaurants\nSpa and wellness\nBusiness center\nConcierge service\nValet parking",
                Amenities = "Private beach\nInfinity pools\nFull-service spa\nFitness center\nMultiple restaurants\nBeach club\nYacht services"
            },
            new {
                Name = "Santorini Sunset Resort",
                HotelName = "Santorini Sunset Resort",
                Description = "Breathtaking cliffside resort with infinity pools overlooking the Aegean Sea, white-washed architecture, and world-renowned sunsets. A romantic paradise in the Greek Islands.",
                ShortDescription = "Breathtaking cliffside resort with infinity pools and world-renowned sunsets. A romantic paradise in the Greek Islands.",
                Address = "Oia Village",
                City = "Santorini",
                Country = "Greece",
                Phone = "+30 2286 012345",
                Email = "santorini@sunsetresort.com",
                Highlights = "Infinity pools with Aegean views\nWorld-renowned sunset views\nWhite-washed cave suites\nPrivate terraces\nAward-winning Greek restaurant\nWine cellar\nSpa with sea views",
                Features = "Cliffside location\nInfinity pools\nFine dining\nSpa and wellness\nConcierge service\nAirport transfers\nRomantic packages",
                Amenities = "Infinity pools\nPrivate terraces\nSpa facilities\nRestaurant and bar\nWine tasting\nSunset viewing areas\nBeach access"
            }
        };

        var roomType = _contentTypeService.Get("room");
        var offerType = _contentTypeService.Get("offer");

        // Get property aliases once for hotel type
        var hotelPropertyAliases = hotelType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();

        foreach (var hotelData in hotels)
        {
            // Check if hotel already exists
            var existingHotels = _contentService.GetPagedOfType(hotelType.Id, 0, 100, out _, null);
            if (existingHotels.Any(h => h.Name == hotelData.Name))
            {
                continue; // Skip if already exists
            }

            // Create hotel
            var hotel = _contentService.Create(hotelData.Name, root.Key, "hotel");
            
            // Safely set values - only if property exists
            if (hotelPropertyAliases.Contains("hotelName")) hotel.SetValue("hotelName", hotelData.HotelName);
            if (hotelPropertyAliases.Contains("description")) hotel.SetValue("description", hotelData.Description);
            if (hotelPropertyAliases.Contains("shortDescription")) hotel.SetValue("shortDescription", hotelData.ShortDescription);
            if (hotelPropertyAliases.Contains("address")) hotel.SetValue("address", hotelData.Address);
            if (hotelPropertyAliases.Contains("city")) hotel.SetValue("city", hotelData.City);
            if (hotelPropertyAliases.Contains("country")) hotel.SetValue("country", hotelData.Country);
            if (hotelPropertyAliases.Contains("phone")) hotel.SetValue("phone", hotelData.Phone);
            if (hotelPropertyAliases.Contains("email")) hotel.SetValue("email", hotelData.Email);
            if (hotelPropertyAliases.Contains("highlights")) hotel.SetValue("highlights", hotelData.Highlights);
            if (hotelPropertyAliases.Contains("features")) hotel.SetValue("features", hotelData.Features);
            if (hotelPropertyAliases.Contains("amenities")) hotel.SetValue("amenities", hotelData.Amenities);
            if (hotelPropertyAliases.Contains("layout")) hotel.SetValue("layout", "Main");
            
            _contentService.Save(hotel);

            if (roomType != null)
            {
                // Create rooms for each hotel with detailed information
                var rooms = new[]
                {
                    new { 
                        Name = "Deluxe Double Room", 
                        Description = "Spacious room with king-size bed and stunning views. Elegantly appointed with premium furnishings and modern amenities.",
                        MaxOccupancy = 2, 
                        PriceFrom = 180.00m, 
                        RoomType = "Double",
                        Size = "35 m²",
                        BedType = "King-size bed",
                        Features = "City views\nSeparate seating area\nMarble bathroom\nPremium linens\nMinibar\nSmart TV\nComplimentary Wi-Fi",
                        Furnishings = "King-size bed with premium linens\nSeating area with armchairs\nWriting desk\nWardrobe\nMarble bathroom with rain shower\nSeparate bathtub\nDouble vanity",
                        Specifications = "Internal size: 35m²/377ft²\nKing-size bed\nMax occupancy: 2 guests\nBathroom with separate shower and bathtub\nCity views",
                        HotelServices = "24-hour room service\nDaily housekeeping\nTurn-down service\nComplimentary Wi-Fi\nNewspaper delivery\nLaundry service\nConcierge assistance"
                    },
                    new { 
                        Name = "Executive Suite", 
                        Description = "Luxurious suite with separate living area and premium amenities. Perfect for extended stays or business travelers.",
                        MaxOccupancy = 4, 
                        PriceFrom = 350.00m, 
                        RoomType = "Suite",
                        Size = "65 m²",
                        BedType = "King-size bed",
                        Features = "Separate living room\nDining area\nCity or river views\nPremium amenities\nWork desk\nComplimentary minibar\nExpress check-in/out",
                        Furnishings = "King-size bedroom\nSeparate living room with sofa\nDining table for 4\nKitchenette\nStudy area\nWalk-in wardrobe\nMarble bathroom with separate shower and bathtub",
                        Specifications = "Internal size: 65m²/700ft²\nKing-size bed\nMax occupancy: 4 guests\nSeparate living and sleeping areas\nCity or river views",
                        HotelServices = "24-hour room service\nDaily housekeeping\nTurn-down service\nComplimentary Wi-Fi and minibar\nNewspaper delivery\nLaundry and pressing service\nPriority concierge"
                    },
                    new { 
                        Name = "Premium Single Room", 
                        Description = "Elegant single room perfect for business travelers. Thoughtfully designed for comfort and productivity.",
                        MaxOccupancy = 1, 
                        PriceFrom = 120.00m, 
                        RoomType = "Single",
                        Size = "25 m²",
                        BedType = "Single bed",
                        Features = "City views\nErgonomic work desk\nHigh-speed Wi-Fi\nPremium linens\nMinibar\nSmart TV\nExpress check-in",
                        Furnishings = "Single bed with premium linens\nErgonomic work desk and chair\nComfortable armchair\nWardrobe\nMarble bathroom with rain shower\nVanity area",
                        Specifications = "Internal size: 25m²/269ft²\nSingle bed\nMax occupancy: 1 guest\nCity views\nCompact but luxurious",
                        HotelServices = "24-hour room service\nDaily housekeeping\nComplimentary Wi-Fi\nNewspaper delivery\nLaundry service\nBusiness center access"
                    },
                    new { 
                        Name = "Family Suite", 
                        Description = "Spacious family accommodation with multiple bedrooms. Perfect for families traveling together.",
                        MaxOccupancy = 6, 
                        PriceFrom = 450.00m, 
                        RoomType = "Family",
                        Size = "95 m²",
                        BedType = "Multiple beds",
                        Features = "Two bedrooms\nSeparate living area\nKitchenette\nFamily-friendly amenities\nCity views\nExtra beds available\nComplimentary Wi-Fi",
                        Furnishings = "Master bedroom with king-size bed\nSecond bedroom with twin beds\nSeparate living room\nDining area\nKitchenette with appliances\nTwo bathrooms\nStorage space",
                        Specifications = "Internal size: 95m²/1,023ft²\nTwo bedrooms\nMax occupancy: 6 guests\nSeparate living and dining areas\nFamily-friendly layout",
                        HotelServices = "24-hour room service\nDaily housekeeping\nTurn-down service\nComplimentary Wi-Fi\nExtra beds and cribs available\nLaundry service\nFamily concierge"
                    },
                    new { 
                        Name = "Presidential Suite", 
                        Description = "Ultimate luxury experience with private terrace and butler service. The pinnacle of accommodation excellence.",
                        MaxOccupancy = 4, 
                        PriceFrom = 800.00m, 
                        RoomType = "Presidential",
                        Size = "150 m²",
                        BedType = "King-size bed",
                        Features = "Private terrace\nPanoramic views\nButler service\nSeparate living and dining rooms\nPrivate bar\nPremium amenities\nPrivate entrance",
                        Furnishings = "Master bedroom with king-size bed and terrace access\nSeparate living room with fireplace\nFormal dining room for 8\nPrivate bar and wine cellar\nStudy with fireplace\nGuest bathroom\nMaster bathroom with fireplace and separate rain shower\nPrivate terrace with outdoor seating",
                        Specifications = "Internal size: 150m²/1,615ft²\nTerrace size: 30m²/323ft²\nKing-size bed\nMax occupancy: 4 guests\nPrivate terrace with panoramic views",
                        HotelServices = "Dedicated butler service\n24-hour room service\nDaily housekeeping\nTurn-down service\nComplimentary Wi-Fi and minibar\nPremium toiletries\nNewspaper and magazine selection\nLaundry and pressing service\nPriority concierge\nComplimentary airport transfers"
                    }
                };

                foreach (var roomData in rooms)
                {
                    // Ensure unique room name by appending hotel name if needed
                    var roomName = roomData.Name;
                    var existingRooms = _contentService.GetPagedChildren(hotel.Id, 0, 100, out _, null)
                        .Where(r => r.ContentType.Alias == "room" && r.Name == roomName);
                    if (existingRooms.Any())
                    {
                        roomName = $"{roomData.Name} - {hotelData.Name}";
                    }
                    var room = _contentService.Create(roomName, hotel.Key, "room");
                    var roomPropertyAliases = roomType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();
                    
                    if (roomPropertyAliases.Contains("roomName")) room.SetValue("roomName", roomData.Name);
                    if (roomPropertyAliases.Contains("description")) room.SetValue("description", roomData.Description);
                    if (roomPropertyAliases.Contains("maxOccupancy")) room.SetValue("maxOccupancy", roomData.MaxOccupancy);
                    if (roomPropertyAliases.Contains("priceFrom")) room.SetValue("priceFrom", roomData.PriceFrom);
                    if (roomPropertyAliases.Contains("roomType")) room.SetValue("roomType", roomData.RoomType);
                    if (roomPropertyAliases.Contains("size")) room.SetValue("size", roomData.Size);
                    if (roomPropertyAliases.Contains("bedType")) room.SetValue("bedType", roomData.BedType);
                    if (roomPropertyAliases.Contains("features")) room.SetValue("features", roomData.Features);
                    if (roomPropertyAliases.Contains("furnishings")) room.SetValue("furnishings", roomData.Furnishings);
                    if (roomPropertyAliases.Contains("specifications")) room.SetValue("specifications", roomData.Specifications);
                    if (roomPropertyAliases.Contains("hotelServices")) room.SetValue("hotelServices", roomData.HotelServices);
                    
                    _contentService.Save(room);
                }
            }

            if (offerType != null)
            {
                // Create offers for each hotel
                var offers = new[]
                {
                    new { Name = "Early Bird Special", Description = "Book 30 days in advance and save 20% on your stay", Discount = 20.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(6) },
                    new { Name = "Weekend Getaway", Description = "Special weekend rates with complimentary breakfast and spa access", Discount = 15.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(3) },
                    new { Name = "Long Stay Discount", Description = "Stay 7+ nights and get 25% off plus free airport transfers", Discount = 25.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(12) },
                    new { Name = "Honeymoon Package", Description = "Romantic getaway with champagne, flowers, and late checkout", Discount = 30.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(12) }
                };

                foreach (var offerData in offers)
                {
                    // Ensure unique offer name by appending hotel name if needed
                    var offerName = offerData.Name;
                    var existingOffers = _contentService.GetPagedChildren(hotel.Id, 0, 100, out _, null)
                        .Where(o => o.ContentType.Alias == "offer" && o.Name == offerName);
                    if (existingOffers.Any())
                    {
                        offerName = $"{offerData.Name} - {hotelData.Name}";
                    }
                    var offer = _contentService.Create(offerName, hotel.Key, "offer");
                    var offerPropertyAliases = offerType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();
                    
                    if (offerPropertyAliases.Contains("offerName")) offer.SetValue("offerName", offerData.Name);
                    if (offerPropertyAliases.Contains("description")) offer.SetValue("description", offerData.Description);
                    if (offerPropertyAliases.Contains("discount")) offer.SetValue("discount", offerData.Discount);
                    if (offerPropertyAliases.Contains("validFrom")) offer.SetValue("validFrom", offerData.ValidFrom);
                    if (offerPropertyAliases.Contains("validTo")) offer.SetValue("validTo", offerData.ValidTo);
                    
                    _contentService.Save(offer);
                }
            }

            // Create events for each hotel
            var eventType = _contentTypeService.Get("event");
            if (eventType != null)
            {
                var events = new[]
                {
                    new { 
                        Name = "Wine Tasting Evening", 
                        Description = "Join us for an exclusive wine tasting event featuring local and international selections. Includes canapés and expert sommelier guidance.", 
                        EventDate = DateTime.Today.AddDays(14), 
                        Location = "Hotel Restaurant", 
                        Price = 75.00m 
                    },
                    new { 
                        Name = "Live Jazz Night", 
                        Description = "Enjoy an evening of smooth jazz performances in our elegant lounge. Complimentary welcome drink included.", 
                        EventDate = DateTime.Today.AddDays(21), 
                        Location = "Rooftop Bar", 
                        Price = 45.00m 
                    },
                    new { 
                        Name = "Cooking Masterclass", 
                        Description = "Learn from our award-winning chef in an interactive cooking masterclass. Includes lunch and recipe cards to take home.", 
                        EventDate = DateTime.Today.AddDays(28), 
                        Location = "Chef's Kitchen", 
                        Price = 120.00m 
                    },
                    new { 
                        Name = "Wellness Workshop", 
                        Description = "A relaxing wellness workshop focusing on mindfulness and meditation. Includes spa access and healthy refreshments.", 
                        EventDate = DateTime.Today.AddDays(35), 
                        Location = "Spa & Wellness Center", 
                        Price = 65.00m 
                    }
                };

                foreach (var eventData in events)
                {
                    // Ensure unique event name by appending hotel name if needed
                    var eventName = eventData.Name;
                    var existingEvents = _contentService.GetPagedChildren(hotel.Id, 0, 100, out _, null)
                        .Where(e => e.ContentType.Alias == "event" && e.Name == eventName);
                    if (existingEvents.Any())
                    {
                        eventName = $"{eventData.Name} - {hotelData.Name}";
                    }
                    var evt = _contentService.Create(eventName, hotel.Key, "event");
                    var eventPropertyAliases = eventType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();
                    
                    if (eventPropertyAliases.Contains("eventName")) evt.SetValue("eventName", eventData.Name);
                    if (eventPropertyAliases.Contains("description")) evt.SetValue("description", eventData.Description);
                    if (eventPropertyAliases.Contains("eventDate")) evt.SetValue("eventDate", eventData.EventDate);
                    if (eventPropertyAliases.Contains("location")) evt.SetValue("location", eventData.Location);
                    if (eventPropertyAliases.Contains("price")) evt.SetValue("price", eventData.Price);
                    
                    _contentService.Save(evt);
                }
            }
        }
    }

    public void AddOffersAndEventsToExistingHotels()
    {
        var hotelType = _contentTypeService.Get("hotel");
        if (hotelType == null) return;

        var offerType = _contentTypeService.Get("offer");
        var eventType = _contentTypeService.Get("event");

        var hotels = _contentService.GetPagedOfType(hotelType.Id, 0, int.MaxValue, out _, null);

        foreach (var hotel in hotels)
        {
            // Add offers if offer type exists
            if (offerType != null)
            {
                var offers = new[]
                {
                    new { Name = "Early Bird Special", Description = "Book 30 days in advance and save 20% on your stay", Discount = 20.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(6), MinAdvanceBookingDays = 30, MinNights = 0 },
                    new { Name = "Weekend Getaway", Description = "Special weekend rates with complimentary breakfast and spa access", Discount = 15.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(3), MinAdvanceBookingDays = 0, MinNights = 0 },
                    new { Name = "Long Stay Discount", Description = "Stay 5+ nights and get 25% off plus free airport transfers", Discount = 25.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(12), MinAdvanceBookingDays = 0, MinNights = 5 }
                };

                foreach (var offerData in offers)
                {
                    var offerName = offerData.Name;
                    var existingOffers = _contentService.GetPagedChildren(hotel.Id, 0, 100, out _, null)
                        .Where(o => o.ContentType.Alias == "offer" && o.Name == offerName);
                    if (existingOffers.Any())
                    {
                        continue; // Skip if already exists
                    }

                    var offer = _contentService.Create(offerName, hotel.Key, "offer");
                    var offerPropertyAliases = offerType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();
                    
                    if (offerPropertyAliases.Contains("offerName")) offer.SetValue("offerName", offerData.Name);
                    if (offerPropertyAliases.Contains("description")) offer.SetValue("description", offerData.Description);
                    if (offerPropertyAliases.Contains("discount")) offer.SetValue("discount", offerData.Discount);
                    if (offerPropertyAliases.Contains("validFrom")) offer.SetValue("validFrom", offerData.ValidFrom);
                    if (offerPropertyAliases.Contains("validTo")) offer.SetValue("validTo", offerData.ValidTo);
                    if (offerPropertyAliases.Contains("minAdvanceBookingDays")) offer.SetValue("minAdvanceBookingDays", offerData.MinAdvanceBookingDays);
                    if (offerPropertyAliases.Contains("minNights")) offer.SetValue("minNights", offerData.MinNights);
                    
                    _contentService.Save(offer);
                }
            }

            // Add events if event type exists
            if (eventType != null)
            {
                var events = new[]
                {
                    new { 
                        Name = "Wine Tasting Evening", 
                        Description = "Join us for an exclusive wine tasting event featuring local and international selections. Includes canapés and expert sommelier guidance.", 
                        EventDate = DateTime.Today.AddDays(14), 
                        Location = "Hotel Restaurant", 
                        Price = 75.00m 
                    },
                    new { 
                        Name = "Live Jazz Night", 
                        Description = "Enjoy an evening of smooth jazz performances in our elegant lounge. Complimentary welcome drink included.", 
                        EventDate = DateTime.Today.AddDays(21), 
                        Location = "Rooftop Bar", 
                        Price = 45.00m 
                    },
                    new { 
                        Name = "Cooking Masterclass", 
                        Description = "Learn from our award-winning chef in an interactive cooking masterclass. Includes lunch and recipe cards to take home.", 
                        EventDate = DateTime.Today.AddDays(28), 
                        Location = "Chef's Kitchen", 
                        Price = 120.00m 
                    }
                };

                foreach (var eventData in events)
                {
                    var eventName = eventData.Name;
                    var existingEvents = _contentService.GetPagedChildren(hotel.Id, 0, 100, out _, null)
                        .Where(e => e.ContentType.Alias == "event" && e.Name == eventName);
                    if (existingEvents.Any())
                    {
                        continue; // Skip if already exists
                    }

                    var evt = _contentService.Create(eventName, hotel.Key, "event");
                    var eventPropertyAliases = eventType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();
                    
                    if (eventPropertyAliases.Contains("eventName")) evt.SetValue("eventName", eventData.Name);
                    if (eventPropertyAliases.Contains("description")) evt.SetValue("description", eventData.Description);
                    if (eventPropertyAliases.Contains("eventDate")) evt.SetValue("eventDate", eventData.EventDate);
                    if (eventPropertyAliases.Contains("location")) evt.SetValue("location", eventData.Location);
                    if (eventPropertyAliases.Contains("price")) evt.SetValue("price", eventData.Price);
                    
                    _contentService.Save(evt);
                }
            }
        }
    }
}

