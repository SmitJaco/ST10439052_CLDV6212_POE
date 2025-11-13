using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Models.ViewModels;

namespace ST10439052_CLDV_POE.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterViewModel model);
        Task<User?> LoginAsync(string username, string password);
        Task<User?> GetUserAsync(string username);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        Task<bool> IsAdminAsync(string username);
    }
}

