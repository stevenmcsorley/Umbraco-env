using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

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

        // Check if example hotel already exists
        var existingHotels = _contentService.GetPagedOfType(hotelType.Id, 0, 10, out _, null);
        if (existingHotels.Any(h => h.Name == "Grand Hotel Example"))
        {
            return; // Already seeded
        }

        var root = _contentService.GetRootContent().FirstOrDefault();
        if (root == null) return;

        // Create hotel
        var hotel = _contentService.Create("Grand Hotel Example", root.Key, "hotel");
        hotel.SetValue("hotelName", "Grand Hotel Example");
        hotel.SetValue("description", "A luxurious hotel located in the heart of the city, offering world-class amenities and exceptional service.");
        hotel.SetValue("address", "123 Main Street");
        hotel.SetValue("city", "London");
        hotel.SetValue("country", "United Kingdom");
        hotel.SetValue("phone", "+44 20 1234 5678");
        hotel.SetValue("email", "info@grandhotel.example");
        _contentService.Save(hotel);

        var roomType = _contentTypeService.Get("room");
        if (roomType != null)
        {
            // Create rooms
            var rooms = new[]
            {
                new { Name = "Deluxe Double Room", Description = "Spacious room with king-size bed and city view", MaxOccupancy = 2, PriceFrom = 150.00m, RoomType = "Double" },
                new { Name = "Executive Suite", Description = "Luxurious suite with separate living area", MaxOccupancy = 4, PriceFrom = 300.00m, RoomType = "Suite" },
                new { Name = "Standard Single Room", Description = "Comfortable single room perfect for solo travelers", MaxOccupancy = 1, PriceFrom = 100.00m, RoomType = "Single" },
                new { Name = "Family Room", Description = "Large room with multiple beds, ideal for families", MaxOccupancy = 5, PriceFrom = 250.00m, RoomType = "Family" }
            };

            foreach (var roomData in rooms)
            {
                var room = _contentService.Create(roomData.Name, hotel.Key, "room");
                room.SetValue("roomName", roomData.Name);
                room.SetValue("description", roomData.Description);
                room.SetValue("maxOccupancy", roomData.MaxOccupancy);
                room.SetValue("priceFrom", roomData.PriceFrom);
                room.SetValue("roomType", roomData.RoomType);
                _contentService.Save(room);
            }
        }

        var offerType = _contentTypeService.Get("offer");
        if (offerType != null)
        {
            // Create offers
            var offers = new[]
            {
                new { Name = "Early Bird Special", Description = "Book 30 days in advance and save 20%", Discount = 20.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(6) },
                new { Name = "Weekend Getaway", Description = "Special weekend rates with complimentary breakfast", Discount = 15.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(3) },
                new { Name = "Long Stay Discount", Description = "Stay 7+ nights and get 25% off", Discount = 25.00m, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddMonths(12) }
            };

            foreach (var offerData in offers)
            {
                var offer = _contentService.Create(offerData.Name, hotel.Key, "offer");
                offer.SetValue("offerName", offerData.Name);
                offer.SetValue("description", offerData.Description);
                offer.SetValue("discount", offerData.Discount);
                offer.SetValue("validFrom", offerData.ValidFrom);
                offer.SetValue("validTo", offerData.ValidTo);
                _contentService.Save(offer);
            }
        }
    }
}

