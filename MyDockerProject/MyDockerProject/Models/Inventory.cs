using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDockerProject.Models;

[Table("Inventory")]
public class Inventory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid ProductId { get; set; } // Room ID or Event ID
    
    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty; // "Room" or "Event"
    
    [Required]
    public DateTime Date { get; set; } // The specific date for this inventory entry
    
    [Required]
    public int TotalQuantity { get; set; } // Total available (e.g., number of rooms or event seats)
    
    [Required]
    public int BookedQuantity { get; set; } = 0; // Currently booked
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; } // Price for this date
    
    [MaxLength(10)]
    public string Currency { get; set; } = "GBP";
    
    public bool IsAvailable { get; set; } = true; // Can be manually disabled
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Computed property (not stored in DB)
    [NotMapped]
    public int AvailableQuantity => TotalQuantity - BookedQuantity;
    
}

