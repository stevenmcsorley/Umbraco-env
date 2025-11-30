using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDockerProject.Models;

[Table("Bookings")]
public class Booking
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string BookingReference { get; set; } = string.Empty;
    
    [Required]
    public Guid ProductId { get; set; } // Room or Event ID
    
    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty; // "Room" or "Event"
    
    [Required]
    public DateTime CheckIn { get; set; } // For rooms: check-in date, for events: event date
    
    public DateTime? CheckOut { get; set; } // For rooms: check-out date, null for events
    
    [Required]
    public int Quantity { get; set; } = 1; // Number of rooms or event tickets
    
    [Required]
    [MaxLength(100)]
    public string GuestFirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string GuestLastName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string GuestEmail { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? GuestPhone { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "GBP";
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled, Completed
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // JSON field for storing add-ons, events, offers applied
    public string? AdditionalData { get; set; } // JSON string
    
    // Foreign key to user (if authenticated)
    public Guid? UserId { get; set; }
}

