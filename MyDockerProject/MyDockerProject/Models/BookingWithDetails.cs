using MyDockerProject.Models;

namespace MyDockerProject.Models;

public class BookingWithDetails
{
    public Booking Booking { get; set; } = null!;
    public string? ProductName { get; set; }
    public string? HotelName { get; set; }
    public string? HotelLocation { get; set; }
    public string? RoomImage { get; set; }
    public string? HotelImage { get; set; }
    public List<string>? RoomFeatures { get; set; }
    public List<EventDetail>? Events { get; set; }
    public List<object>? AddOns { get; set; }
}

public class EventDetail
{
    public string EventId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime? Date { get; set; }
    public decimal? Price { get; set; }
}

