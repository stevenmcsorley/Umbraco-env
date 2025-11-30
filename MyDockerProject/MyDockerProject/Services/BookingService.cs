using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Models;

namespace MyDockerProject.Services;

public class BookingService
{
    private readonly BookingDbContext _context;
    
    public BookingService(BookingDbContext context)
    {
        _context = context;
    }
    
    public async Task<Booking> CreateBookingAsync(
        Guid productId,
        string productType,
        DateTime checkIn,
        DateTime? checkOut,
        int quantity,
        string firstName,
        string lastName,
        string email,
        string? phone,
        decimal totalPrice,
        string currency = "GBP",
        string? additionalData = null,
        Guid? userId = null)
    {
        // Generate unique booking reference
        var bookingRef = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        
        var booking = new Booking
        {
            BookingReference = bookingRef,
            ProductId = productId,
            ProductType = productType,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Quantity = quantity,
            GuestFirstName = firstName,
            GuestLastName = lastName,
            GuestEmail = email,
            GuestPhone = phone,
            TotalPrice = totalPrice,
            Currency = currency,
            Status = "Confirmed",
            AdditionalData = additionalData,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Bookings.Add(booking);
        
        // Update inventory
        await UpdateInventoryForBookingAsync(productId, productType, checkIn, checkOut, quantity);
        
        await _context.SaveChangesAsync();
        
        return booking;
    }
    
    private async Task UpdateInventoryForBookingAsync(
        Guid productId,
        string productType,
        DateTime checkIn,
        DateTime? checkOut,
        int quantity)
    {
        if (productType == "Room" && checkOut.HasValue)
        {
            // For rooms, update inventory for each day in the date range
            var currentDate = checkIn.Date;
            var endDate = checkOut.Value.Date;
            
            while (currentDate < endDate)
            {
                var inventory = await _context.Inventory
                    .FirstOrDefaultAsync(i => i.ProductId == productId && i.Date == currentDate);
                
                if (inventory != null)
                {
                    inventory.BookedQuantity += quantity;
                    inventory.UpdatedAt = DateTime.UtcNow;
                    
                    // Ensure we don't overbook
                    if (inventory.BookedQuantity > inventory.TotalQuantity)
                    {
                        throw new InvalidOperationException(
                            $"Cannot book {quantity} units. Only {inventory.AvailableQuantity} available on {currentDate:yyyy-MM-dd}");
                    }
                }
                
                currentDate = currentDate.AddDays(1);
            }
        }
        else if (productType == "Event")
        {
            // For events, update inventory for the specific date
            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.Date == checkIn.Date);
            
            if (inventory != null)
            {
                inventory.BookedQuantity += quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                
                if (inventory.BookedQuantity > inventory.TotalQuantity)
                {
                    throw new InvalidOperationException(
                        $"Cannot book {quantity} tickets. Only {inventory.AvailableQuantity} available on {checkIn:yyyy-MM-dd}");
                }
            }
        }
    }
    
    public async Task<List<Booking>> GetBookingsByUserAsync(Guid userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Booking?> GetBookingByReferenceAsync(string bookingReference)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);
    }
    
    public async Task CancelBookingAsync(string bookingReference)
    {
        var booking = await GetBookingByReferenceAsync(bookingReference);
        if (booking == null || booking.Status == "Cancelled")
        {
            return;
        }
        
        booking.Status = "Cancelled";
        booking.UpdatedAt = DateTime.UtcNow;
        
        // Release inventory
        await ReleaseInventoryForBookingAsync(booking);
        
        await _context.SaveChangesAsync();
    }
    
    private async Task ReleaseInventoryForBookingAsync(Booking booking)
    {
        if (booking.ProductType == "Room" && booking.CheckOut.HasValue)
        {
            var currentDate = booking.CheckIn.Date;
            var endDate = booking.CheckOut.Value.Date;
            
            while (currentDate < endDate)
            {
                var inventory = await _context.Inventory
                    .FirstOrDefaultAsync(i => i.ProductId == booking.ProductId && i.Date == currentDate);
                
                if (inventory != null)
                {
                    inventory.BookedQuantity = Math.Max(0, inventory.BookedQuantity - booking.Quantity);
                    inventory.UpdatedAt = DateTime.UtcNow;
                }
                
                currentDate = currentDate.AddDays(1);
            }
        }
        else if (booking.ProductType == "Event")
        {
            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductId == booking.ProductId && i.Date == booking.CheckIn.Date);
            
            if (inventory != null)
            {
                inventory.BookedQuantity = Math.Max(0, inventory.BookedQuantity - booking.Quantity);
                inventory.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

