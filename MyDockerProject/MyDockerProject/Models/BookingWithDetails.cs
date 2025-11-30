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
}

