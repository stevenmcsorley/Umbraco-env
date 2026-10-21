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
    public DbSet<SiteKitOperationalRecord> SiteKitOperationalRecords { get; set; }
    
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

        modelBuilder.Entity<SiteKitOperationalRecord>(entity =>
        {
            entity.ToTable("SiteKitOperationalRecord");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).HasMaxLength(32);
            entity.Property(record => record.ProjectId).HasMaxLength(64).IsRequired();
            entity.Property(record => record.EntityAlias).HasMaxLength(64).IsRequired();
            entity.Property(record => record.DataJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(record => record.CreatedBy).HasMaxLength(320);
            entity.Property(record => record.UpdatedBy).HasMaxLength(320);
            entity.HasIndex(record => new { record.ProjectId, record.EntityAlias, record.UpdatedAt });
        });
    }
}

