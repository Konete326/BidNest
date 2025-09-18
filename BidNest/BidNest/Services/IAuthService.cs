using BidNest.Models;

namespace BidNest.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> RegisterAsync(string username, string email, string password, string fullName, int roleId = 2);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}
