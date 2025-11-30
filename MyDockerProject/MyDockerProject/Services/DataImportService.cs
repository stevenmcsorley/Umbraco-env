using System.Text.Json;
using MyDockerProject.Data;
using MyDockerProject.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace MyDockerProject.Services;

public class DataImportService
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaService _mediaService;
    private readonly BookingDbContext _dbContext;
    private readonly InventoryService _inventoryService;
    
    public DataImportService(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IMediaService mediaService,
        BookingDbContext dbContext,
        InventoryService inventoryService)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _mediaService = mediaService;
        _dbContext = dbContext;
        _inventoryService = inventoryService;
    }
    
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int HotelsCreated { get; set; }
        public int HotelsUpdated { get; set; }
        public int RoomsCreated { get; set; }
        public int RoomsUpdated { get; set; }
        public int EventsCreated { get; set; }
        public int EventsUpdated { get; set; }
        public int OffersCreated { get; set; }
        public int OffersUpdated { get; set; }
        public int InventoryEntriesCreated { get; set; }
        public int InventoryEntriesUpdated { get; set; }
        public List<string> Errors { get; set; } = new();
    }
    
    public class ImportData
    {
        public List<HotelImport>? Hotels { get; set; }
    }
    
    public class HotelImport
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ImageUrl { get; set; }
        public List<RoomImport>? Rooms { get; set; }
        public List<EventImport>? Events { get; set; }
        public List<OfferImport>? Offers { get; set; }
    }
    
    public class RoomImport
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public int? MaxOccupancy { get; set; }
        public string? BedType { get; set; }
        public string? Size { get; set; }
        public string? ImageUrl { get; set; }
        public Dictionary<string, decimal>? Prices { get; set; } // Date -> Price
        public Dictionary<string, int>? Availability { get; set; } // Date -> Available quantity
    }
    
    public class EventImport
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public DateTime? EventDate { get; set; }
        public string? Location { get; set; }
        public decimal? Price { get; set; }
        public int? Capacity { get; set; } // Total seats available
        public string? ImageUrl { get; set; }
    }
    
    public class OfferImport
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public decimal? Discount { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int? MinNights { get; set; }
        public bool? RequiresWeekend { get; set; }
        public int? AdvanceBookingDays { get; set; }
        public string? ImageUrl { get; set; }
    }
    
    public async Task<ImportResult> ImportFromJsonAsync(string jsonData)
    {
        var result = new ImportResult();
        
        try
        {
            var importData = JsonSerializer.Deserialize<ImportData>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (importData?.Hotels == null || importData.Hotels.Count == 0)
            {
                result.Success = false;
                result.Message = "No hotels found in import data";
                return result;
            }
            
            // Get hotel document type
            var hotelType = _contentTypeService.Get("hotel");
            if (hotelType == null)
            {
                result.Success = false;
                result.Message = "Hotel document type not found. Please create it first.";
                return result;
            }
            
            // Get root content
            var rootContent = _contentService.GetRootContent().FirstOrDefault();
            if (rootContent == null)
            {
                result.Success = false;
                result.Message = "No root content found. Please set up Umbraco first.";
                return result;
            }
            
            foreach (var hotelData in importData.Hotels)
            {
                try
                {
                    var (hotel, hotelWasCreated) = await ImportHotelAsync(hotelData, hotelType, rootContent.Key);
                    if (hotel != null)
                    {
                        if (hotelWasCreated)
                            result.HotelsCreated++;
                        else
                            result.HotelsUpdated++;
                        
                        // Import rooms
                        if (hotelData.Rooms != null)
                        {
                            foreach (var roomData in hotelData.Rooms)
                            {
                                try
                                {
                                    var (room, roomWasCreated) = await ImportRoomAsync(roomData, hotel.Key);
                                    if (room != null)
                                    {
                                        if (roomWasCreated)
                                            result.RoomsCreated++;
                                        else
                                            result.RoomsUpdated++;
                                        
                                        // Import inventory and pricing (always updates existing inventory)
                                        if (roomData.Prices != null || roomData.Availability != null)
                                        {
                                            var (created, updated) = await ImportRoomInventoryAsync(room.Key, roomData);
                                            result.InventoryEntriesCreated += created;
                                            result.InventoryEntriesUpdated += updated;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    result.Errors.Add($"Error importing room {roomData.Name}: {ex.Message}");
                                }
                            }
                        }
                        
                        // Import events
                        if (hotelData.Events != null)
                        {
                            foreach (var eventData in hotelData.Events)
                            {
                                try
                                {
                                    var (eventItem, eventWasCreated) = await ImportEventAsync(eventData, hotel.Key);
                                    if (eventItem != null)
                                    {
                                        if (eventWasCreated)
                                            result.EventsCreated++;
                                        else
                                            result.EventsUpdated++;
                                        
                                        // Import event inventory (always updates existing inventory)
                                        if (eventData.EventDate.HasValue && eventData.Capacity.HasValue && eventData.Price.HasValue)
                                        {
                                            var inventoryExists = await _inventoryService.GetInventoryForDateAsync(
                                                eventItem.Key, eventData.EventDate.Value) != null;
                                            
                                            await _inventoryService.SetInventoryAsync(
                                                eventItem.Key,
                                                "Event",
                                                eventData.EventDate.Value,
                                                eventData.Capacity.Value,
                                                eventData.Price.Value
                                            );
                                            
                                            if (inventoryExists)
                                                result.InventoryEntriesUpdated++;
                                            else
                                                result.InventoryEntriesCreated++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    result.Errors.Add($"Error importing event {eventData.Name}: {ex.Message}");
                                }
                            }
                        }
                        
                        // Import offers
                        if (hotelData.Offers != null)
                        {
                            foreach (var offerData in hotelData.Offers)
                            {
                                try
                                {
                                    var (offer, offerWasCreated) = await ImportOfferAsync(offerData, hotel.Key);
                                    if (offer != null)
                                    {
                                        if (offerWasCreated)
                                            result.OffersCreated++;
                                        else
                                            result.OffersUpdated++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    result.Errors.Add($"Error importing offer {offerData.Name}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error importing hotel {hotelData.Name}: {ex.Message}");
                }
            }
            
            result.Success = true;
            var parts = new List<string>();
            if (result.HotelsCreated > 0) parts.Add($"{result.HotelsCreated} hotels created");
            if (result.HotelsUpdated > 0) parts.Add($"{result.HotelsUpdated} hotels updated");
            if (result.RoomsCreated > 0) parts.Add($"{result.RoomsCreated} rooms created");
            if (result.RoomsUpdated > 0) parts.Add($"{result.RoomsUpdated} rooms updated");
            if (result.EventsCreated > 0) parts.Add($"{result.EventsCreated} events created");
            if (result.EventsUpdated > 0) parts.Add($"{result.EventsUpdated} events updated");
            if (result.OffersCreated > 0) parts.Add($"{result.OffersCreated} offers created");
            if (result.OffersUpdated > 0) parts.Add($"{result.OffersUpdated} offers updated");
            if (result.InventoryEntriesCreated > 0) parts.Add($"{result.InventoryEntriesCreated} inventory entries created");
            if (result.InventoryEntriesUpdated > 0) parts.Add($"{result.InventoryEntriesUpdated} inventory entries updated");
            
            result.Message = "Import completed. " + (parts.Count > 0 ? string.Join(", ", parts) : "No changes.");
            
            if (result.Errors.Count > 0)
            {
                result.Message += $" {result.Errors.Count} errors occurred.";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.ToString());
        }
        
        return result;
    }
    
    private async Task<(IContent?, bool wasCreated)> ImportHotelAsync(HotelImport data, IContentType hotelType, Guid parentId)
    {
        if (string.IsNullOrEmpty(data.Name))
        {
            return (null, false);
        }
        
        var slug = data.Slug ?? data.Name.ToLower().Replace(" ", "-");
        
        // Check if hotel already exists - use GetPagedOfType for better performance
        IContent? existing = null;
        
        // Get all hotels of this type
        var allHotels = _contentService.GetPagedOfType(hotelType.Id, 0, int.MaxValue, out _, null)
            .Where(h => h.ContentType.Alias == "hotel");
        
        // First try by hotelName property (case-insensitive)
        existing = allHotels.FirstOrDefault(h => 
        {
            var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
            return hotelName.Equals(data.Name, StringComparison.OrdinalIgnoreCase);
        });
        
        // If not found by hotelName, try by Name property
        if (existing == null)
        {
            existing = allHotels.FirstOrDefault(h => 
                h.Name.Equals(data.Name, StringComparison.OrdinalIgnoreCase));
        }
        
        // If still not found and we have city, try matching by name + city
        if (existing == null && !string.IsNullOrEmpty(data.City))
        {
            existing = allHotels.FirstOrDefault(h => 
            {
                var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
                var city = h.GetValue<string>("city") ?? "";
                return hotelName.Equals(data.Name, StringComparison.OrdinalIgnoreCase) &&
                       city.Equals(data.City, StringComparison.OrdinalIgnoreCase);
            });
        }
        
        IContent hotel;
        bool wasCreated = existing == null;
        
        if (wasCreated)
        {
            // Create new hotel
            hotel = _contentService.Create(data.Name, parentId, hotelType.Alias);
            hotel.SetValue("hotelName", data.Name);
        }
        else
        {
            // Update existing hotel
            hotel = existing!;
        }
        
        // Update properties (works for both new and existing)
        if (!string.IsNullOrEmpty(data.Description)) hotel.SetValue("description", data.Description);
        // Note: "location" property doesn't exist in hotel document type, using address/city/country instead
        if (!string.IsNullOrEmpty(data.Address)) hotel.SetValue("address", data.Address);
        if (!string.IsNullOrEmpty(data.City)) hotel.SetValue("city", data.City);
        if (!string.IsNullOrEmpty(data.Country)) hotel.SetValue("country", data.Country);
        
        _contentService.Save(hotel);
        _contentService.Publish(hotel, cultures: Array.Empty<string>(), userId: 0);
        
        return (hotel, wasCreated);
    }
    
    private async Task<(IContent?, bool wasCreated)> ImportRoomAsync(RoomImport data, Guid hotelId)
    {
        if (string.IsNullOrEmpty(data.Name))
        {
            return (null, false);
        }
        
        var roomType = _contentTypeService.Get("room");
        if (roomType == null) return (null, false);
        
        // Check if room already exists (by name under this hotel)
        // First get the hotel to get its Id (int) from Key (Guid)
        var hotel = _contentService.GetById(hotelId);
        if (hotel == null) return (null, false);
        
        var existingRooms = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out _)
            .Where(r => r.ContentType.Alias == "room");
        
        var existing = existingRooms.FirstOrDefault(r => 
            (r.GetValue<string>("roomName") ?? r.Name).Equals(data.Name, StringComparison.OrdinalIgnoreCase));
        
        IContent room;
        bool wasCreated = existing == null;
        
        if (wasCreated)
        {
            // Create new room
            room = _contentService.Create(data.Name, hotel.Id, roomType.Alias);
            room.SetValue("roomName", data.Name);
        }
        else
        {
            // Update existing room
            room = existing!;
        }
        
        // Update properties (works for both new and existing)
        if (!string.IsNullOrEmpty(data.Description)) room.SetValue("description", data.Description);
        if (data.MaxOccupancy.HasValue) room.SetValue("maxOccupancy", data.MaxOccupancy.Value);
        if (!string.IsNullOrEmpty(data.BedType)) room.SetValue("bedType", data.BedType);
        if (!string.IsNullOrEmpty(data.Size)) room.SetValue("size", data.Size);
        
        _contentService.Save(room);
        _contentService.Publish(room, cultures: Array.Empty<string>(), userId: 0);
        
        return (room, wasCreated);
    }
    
    private async Task<(int created, int updated)> ImportRoomInventoryAsync(Guid roomId, RoomImport data)
    {
        var inventoryData = new Dictionary<DateTime, (int, decimal)>();
        
        // Combine prices and availability
        if (data.Prices != null)
        {
            foreach (var (dateStr, price) in data.Prices)
            {
                if (DateTime.TryParse(dateStr, out var date))
                {
                    var quantity = data.Availability?.ContainsKey(dateStr) == true 
                        ? data.Availability[dateStr] 
                        : 1; // Default to 1 room available
                    
                    inventoryData[date] = (quantity, price);
                }
            }
        }
        else if (data.Availability != null)
        {
            // If only availability is provided, use a default price
            foreach (var (dateStr, quantity) in data.Availability)
            {
                if (DateTime.TryParse(dateStr, out var date))
                {
                    inventoryData[date] = (quantity, 100m); // Default price
                }
            }
        }
        
        int created = 0;
        int updated = 0;
        
        // Set inventory for each date and track created vs updated
        foreach (var (date, (quantity, price)) in inventoryData)
        {
            var existing = await _inventoryService.GetInventoryForDateAsync(roomId, date);
            await _inventoryService.SetInventoryAsync(roomId, "Room", date, quantity, price);
            
            if (existing != null)
                updated++;
            else
                created++;
        }
        
        return (created, updated);
    }
    
    private async Task<(IContent?, bool wasCreated)> ImportEventAsync(EventImport data, Guid hotelId)
    {
        if (string.IsNullOrEmpty(data.Name) || !data.EventDate.HasValue)
        {
            return (null, false);
        }
        
        var eventType = _contentTypeService.Get("event");
        if (eventType == null) return (null, false);
        
        // Check if event already exists (by name under this hotel)
        // First get the hotel to get its Id (int) from Key (Guid)
        var hotel = _contentService.GetById(hotelId);
        if (hotel == null) return (null, false);
        
        var existingEvents = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out _)
            .Where(e => e.ContentType.Alias == "event");
        
        var existing = existingEvents.FirstOrDefault(e => 
            (e.GetValue<string>("eventName") ?? e.Name).Equals(data.Name, StringComparison.OrdinalIgnoreCase));
        
        IContent eventItem;
        bool wasCreated = existing == null;
        
        if (wasCreated)
        {
            eventItem = _contentService.Create(data.Name, hotel.Id, eventType.Alias);
            eventItem.SetValue("eventName", data.Name);
        }
        else
        {
            eventItem = existing!;
        }
        
        // Update properties (works for both new and existing)
        if (!string.IsNullOrEmpty(data.Description)) eventItem.SetValue("description", data.Description);
        if (data.EventDate.HasValue) eventItem.SetValue("eventDate", data.EventDate.Value);
        if (!string.IsNullOrEmpty(data.Location)) eventItem.SetValue("location", data.Location);
        if (data.Price.HasValue) eventItem.SetValue("price", data.Price.Value);
        
        _contentService.Save(eventItem);
        _contentService.Publish(eventItem, cultures: Array.Empty<string>(), userId: 0);
        
        return (eventItem, wasCreated);
    }
    
    private async Task<(IContent?, bool wasCreated)> ImportOfferAsync(OfferImport data, Guid hotelId)
    {
        if (string.IsNullOrEmpty(data.Name))
        {
            return (null, false);
        }
        
        var offerType = _contentTypeService.Get("offer");
        if (offerType == null) return (null, false);
        
        // Check if offer already exists (by name under this hotel)
        // First get the hotel to get its Id (int) from Key (Guid)
        var hotel = _contentService.GetById(hotelId);
        if (hotel == null) return (null, false);
        
        var existingOffers = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out _)
            .Where(o => o.ContentType.Alias == "offer");
        
        var existing = existingOffers.FirstOrDefault(o => 
            (o.GetValue<string>("offerName") ?? o.Name).Equals(data.Name, StringComparison.OrdinalIgnoreCase));
        
        IContent offer;
        bool wasCreated = existing == null;
        
        if (wasCreated)
        {
            offer = _contentService.Create(data.Name, hotel.Id, offerType.Alias);
            offer.SetValue("offerName", data.Name);
        }
        else
        {
            offer = existing!;
        }
        
        // Update properties (works for both new and existing)
        if (!string.IsNullOrEmpty(data.Description)) offer.SetValue("description", data.Description);
        if (data.Discount.HasValue) offer.SetValue("discount", data.Discount.Value);
        if (data.ValidFrom.HasValue) offer.SetValue("validFrom", data.ValidFrom.Value);
        if (data.ValidTo.HasValue) offer.SetValue("validTo", data.ValidTo.Value);
        // Note: minNights, advanceBookingDays, and requiresWeekend properties don't exist in offer document type
        // These would need to be added to the document type if required
        
        _contentService.Save(offer);
        _contentService.Publish(offer, cultures: Array.Empty<string>(), userId: 0);
        
        return (offer, wasCreated);
    }
}

