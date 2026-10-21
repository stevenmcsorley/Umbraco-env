using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Models;

namespace MyDockerProject.Services;

public class UserService : IUserService
{
    private readonly BookingDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserService(BookingDbContext context, ILogger<UserService> logger, PasswordHasher<User> passwordHasher)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
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
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email}", email);
        return user;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
        {
            return null;
        }

        if (IsLegacyHash(user.PasswordHash))
        {
            if (!VerifyLegacyPassword(password, user.PasswordHash))
            {
                return null;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, password);
        }
        else
        {
            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
            }
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

    private bool VerifyLegacyPassword(string password, string hash)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        try
        {
            var legacyHash = Convert.FromBase64String(hash);
            return legacyHash.Length == hashedBytes.Length && CryptographicOperations.FixedTimeEquals(hashedBytes, legacyHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsLegacyHash(string hash)
    {
        try
        {
            return Convert.FromBase64String(hash).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

