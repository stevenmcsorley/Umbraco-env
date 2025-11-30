using MyDockerProject.Models;

namespace MyDockerProject.Services;

public interface IUserService
{
    Task<User?> RegisterAsync(string email, string password, string firstName, string lastName, string? phone = null);
    Task<User?> LoginAsync(string email, string password);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<List<Booking>> GetUserBookingsAsync(Guid userId);
}

