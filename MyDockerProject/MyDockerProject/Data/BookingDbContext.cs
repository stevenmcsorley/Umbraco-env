using Microsoft.EntityFrameworkCore;
using MyDockerProject.Models;

namespace MyDockerProject.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }
    
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Inventory> Inventory { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.BookingReference).IsUnique();
            entity.HasIndex(e => new { e.ProductId, e.CheckIn, e.CheckOut });
            entity.HasIndex(e => e.GuestEmail);
            entity.HasIndex(e => e.CreatedAt);
        });
        
        // Configure Inventory
        modelBuilder.Entity<Inventory>(entity =>
        {
            // Unique constraint: one inventory entry per product per date
            entity.HasIndex(e => new { e.ProductId, e.Date }).IsUnique();
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.ProductId);
        });
        
        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}

