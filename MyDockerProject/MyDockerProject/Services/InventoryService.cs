using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Models;

namespace MyDockerProject.Services;

public class InventoryService
{
    private readonly BookingDbContext _context;
    
    public InventoryService(BookingDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Inventory>> GetInventoryAsync(Guid productId, DateTime fromDate, DateTime toDate)
    {
        return await _context.Inventory
            .Where(i => i.ProductId == productId 
                && i.Date >= fromDate.Date 
                && i.Date <= toDate.Date
                && i.IsAvailable)
            .OrderBy(i => i.Date)
            .ToListAsync();
    }
    
    public async Task<Inventory?> GetInventoryForDateAsync(Guid productId, DateTime date)
    {
        return await _context.Inventory
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.Date == date.Date);
    }
    
    public async Task SetInventoryAsync(
        Guid productId,
        string productType,
        DateTime date,
        int totalQuantity,
        decimal price,
        string currency = "GBP",
        bool isAvailable = true)
    {
        var inventory = await GetInventoryForDateAsync(productId, date);
        
        if (inventory == null)
        {
            inventory = new Inventory
            {
                ProductId = productId,
                ProductType = productType,
                Date = date.Date,
                TotalQuantity = totalQuantity,
                BookedQuantity = 0,
                Price = price,
                Currency = currency,
                IsAvailable = isAvailable,
                CreatedAt = DateTime.UtcNow
            };
            _context.Inventory.Add(inventory);
        }
        else
        {
            // Don't allow reducing total quantity below booked quantity
            if (totalQuantity < inventory.BookedQuantity)
            {
                throw new InvalidOperationException(
                    $"Cannot set total quantity to {totalQuantity}. {inventory.BookedQuantity} units are already booked.");
            }
            
            inventory.TotalQuantity = totalQuantity;
            inventory.Price = price;
            inventory.Currency = currency;
            inventory.IsAvailable = isAvailable;
            inventory.UpdatedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task BulkSetInventoryAsync(
        Guid productId,
        string productType,
        Dictionary<DateTime, (int totalQuantity, decimal price)> inventoryData,
        string currency = "GBP")
    {
        foreach (var (date, (totalQuantity, price)) in inventoryData)
        {
            await SetInventoryAsync(productId, productType, date, totalQuantity, price, currency);
        }
    }
    
    public async Task<bool> IsAvailableAsync(Guid productId, DateTime date, int quantity = 1)
    {
        var inventory = await GetInventoryForDateAsync(productId, date);
        
        if (inventory == null || !inventory.IsAvailable)
        {
            return false;
        }
        
        return inventory.AvailableQuantity >= quantity;
    }
    
    public async Task<bool> IsAvailableForRangeAsync(
        Guid productId,
        DateTime fromDate,
        DateTime toDate,
        int quantity = 1)
    {
        var inventories = await GetInventoryAsync(productId, fromDate, toDate);
        
        // For rooms, check all dates in range
        // For events, only check the fromDate
        var datesToCheck = inventories.Select(i => i.Date).Distinct().ToList();
        
        if (datesToCheck.Count == 0)
        {
            return false; // No inventory set for these dates
        }
        
        foreach (var date in datesToCheck)
        {
            var inventory = inventories.FirstOrDefault(i => i.Date == date);
            if (inventory == null || inventory.AvailableQuantity < quantity)
            {
                return false;
            }
        }
        
        return true;
    }
}


