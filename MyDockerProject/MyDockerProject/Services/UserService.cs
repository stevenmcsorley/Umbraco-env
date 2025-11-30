using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Models;

namespace MyDockerProject.Services;

public class UserService : IUserService
{
    private readonly BookingDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(BookingDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> RegisterAsync(string email, string password, string firstName, string lastName, string? phone = null)
    {
        // Check if user already exists
        var existingUser = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email);
        
        if (existingUser != null)
        {
            return null; // User already exists
        }

        var user = new User
        {
            Email = email,
            PasswordHash = HashPassword(password),
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email}", email);
        return user;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User logged in: {Email}", email);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<List<Booking>> GetUserBookingsAsync(Guid userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }
}

