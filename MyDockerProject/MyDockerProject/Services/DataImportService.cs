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
        public int RoomsCreated { get; set; }
        public int EventsCreated { get; set; }
        public int OffersCreated { get; set; }
        public int InventoryEntriesCreated { get; set; }
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
                    var hotel = await ImportHotelAsync(hotelData, hotelType, rootContent.Key);
                    if (hotel != null)
                    {
                        result.HotelsCreated++;
                        
                        // Import rooms
                        if (hotelData.Rooms != null)
                        {
                            foreach (var roomData in hotelData.Rooms)
                            {
                                try
                                {
                                    var room = await ImportRoomAsync(roomData, hotel.Key);
                                    if (room != null)
                                    {
                                        result.RoomsCreated++;
                                        
                                        // Import inventory and pricing
                                        if (roomData.Prices != null || roomData.Availability != null)
                                        {
                                            await ImportRoomInventoryAsync(room.Key, roomData);
                                            result.InventoryEntriesCreated += roomData.Prices?.Count ?? roomData.Availability?.Count ?? 0;
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
                                    var eventItem = await ImportEventAsync(eventData, hotel.Key);
                                    if (eventItem != null)
                                    {
                                        result.EventsCreated++;
                                        
                                        // Import event inventory
                                        if (eventData.EventDate.HasValue && eventData.Capacity.HasValue && eventData.Price.HasValue)
                                        {
                                            await _inventoryService.SetInventoryAsync(
                                                eventItem.Key,
                                                "Event",
                                                eventData.EventDate.Value,
                                                eventData.Capacity.Value,
                                                eventData.Price.Value
                                            );
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
                                    var offer = await ImportOfferAsync(offerData, hotel.Key);
                                    if (offer != null)
                                    {
                                        result.OffersCreated++;
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
            result.Message = $"Import completed. Created: {result.HotelsCreated} hotels, {result.RoomsCreated} rooms, {result.EventsCreated} events, {result.OffersCreated} offers, {result.InventoryEntriesCreated} inventory entries.";
            
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
    
    private async Task<IContent?> ImportHotelAsync(HotelImport data, IContentType hotelType, Guid parentId)
    {
        if (string.IsNullOrEmpty(data.Name))
        {
            return null;
        }
        
        var slug = data.Slug ?? data.Name.ToLower().Replace(" ", "-");
        
        // Check if hotel already exists
        var existing = _contentService.GetByLevel(1)
            .FirstOrDefault(h => h.GetValue<string>("hotelName") == data.Name);
        
        if (existing != null)
        {
            return existing; // Skip if already exists
        }
        
        var hotel = _contentService.Create(data.Name, parentId, hotelType.Alias);
        hotel.SetValue("hotelName", data.Name);
        if (!string.IsNullOrEmpty(data.Description)) hotel.SetValue("description", data.Description);
        // Note: "location" property doesn't exist in hotel document type, using address/city/country instead
        if (!string.IsNullOrEmpty(data.Address)) hotel.SetValue("address", data.Address);
        if (!string.IsNullOrEmpty(data.City)) hotel.SetValue("city", data.City);
        if (!string.IsNullOrEmpty(data.Country)) hotel.SetValue("country", data.Country);
        
        _contentService.Save(hotel);
        _contentService.Publish(hotel, cultures: Array.Empty<string>(), userId: 0);
        
        return hotel;
    }
    
    private async Task<IContent?> ImportRoomAsync(RoomImport data, Guid hotelId)
    {
        if (string.IsNullOrEmpty(data.Name))
        {
            return null;
        }
        
        var roomType = _contentTypeService.Get("room");
        if (roomType == null) return null;
        
        var room = _contentService.Create(data.Name, hotelId, roomType.Alias);
        room.SetValue("roomName", data.Name);
        if (!string.IsNullOrEmpty(data.Description)) room.SetValue("description", data.Description);
        if (data.MaxOccupancy.HasValue) room.SetValue("maxOccupancy", data.MaxOccupancy.Value);
        if (!string.IsNullOrEmpty(data.BedType)) room.SetValue("bedType", data.BedType);
        if (!string.IsNullOrEmpty(data.Size)) room.SetValue("size", data.Size);
        
        _contentService.Save(room);
        _contentService.Publish(room, cultures: Array.Empty<string>(), userId: 0);
        
        return room;
    }
    
    private async Task ImportRoomInventoryAsync(Guid roomId, RoomImport data)
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
        
        await _inventoryService.BulkSetInventoryAsync(roomId, "Room", inventoryData);
    }
    
    private async Task<IContent?> ImportEventAsync(EventImport data, Guid hotelId)
    {
        if (string.IsNullOrEmpty(data.Name) || !data.EventDate.HasValue)
        {
            return null;
        }
        
        var eventType = _contentTypeService.Get("event");
        if (eventType == null) return null;
        
        var eventItem = _contentService.Create(data.Name, hotelId, eventType.Alias);
        eventItem.SetValue("eventName", data.Name);
        if (!string.IsNullOrEmpty(data.Description)) eventItem.SetValue("description", data.Description);
        if (data.EventDate.HasValue) eventItem.SetValue("eventDate", data.EventDate.Value);
        if (!string.IsNullOrEmpty(data.Location)) eventItem.SetValue("location", data.Location);
        if (data.Price.HasValue) eventItem.SetValue("price", data.Price.Value);
        
        _contentService.Save(eventItem);
        _contentService.Publish(eventItem, cultures: Array.Empty<string>(), userId: 0);
        
        return eventItem;
    }
    
    private async Task<IContent?> ImportOfferAsync(OfferImport data, Guid hotelId)
    {
        if (string.IsNullOrEmpty(data.Name))
        {
            return null;
        }
        
        var offerType = _contentTypeService.Get("offer");
        if (offerType == null) return null;
        
        var offer = _contentService.Create(data.Name, hotelId, offerType.Alias);
        offer.SetValue("offerName", data.Name);
        if (!string.IsNullOrEmpty(data.Description)) offer.SetValue("description", data.Description);
        if (data.Discount.HasValue) offer.SetValue("discount", data.Discount.Value);
        if (data.ValidFrom.HasValue) offer.SetValue("validFrom", data.ValidFrom.Value);
        if (data.ValidTo.HasValue) offer.SetValue("validTo", data.ValidTo.Value);
        // Note: minNights, advanceBookingDays, and requiresWeekend properties don't exist in offer document type
        // These would need to be added to the document type if required
        
        _contentService.Save(offer);
        _contentService.Publish(offer, cultures: Array.Empty<string>(), userId: 0);
        
        return offer;
    }
}

