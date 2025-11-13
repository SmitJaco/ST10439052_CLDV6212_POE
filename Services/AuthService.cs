using Microsoft.EntityFrameworkCore;
using ST10439052_CLDV_POE.Data;
using ST10439052_CLDV_POE.Models;
using ST10439052_CLDV_POE.Models.ViewModels;

namespace ST10439052_CLDV_POE.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _context;

        public AuthService(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            // Check if username already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);
            
            if (existingUser != null)
            {
                return false; // Username already exists
            }

            // Create new user
            var user = new User
            {
                Username = model.Username,
                PasswordHash = HashPassword(model.Password),
                Role = model.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null)
            {
                return null;
            }

            // Verify password
            if (VerifyPassword(password, user.PasswordHash))
            {
                return user;
            }

            return null;
        }

        public async Task<User?> GetUserAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                // If the hash looks like plain text (for existing test users), allow it temporarily
                // This should be removed once all passwords are hashed
                if (hash.Length < 30 && !hash.StartsWith("$2"))
                {
                    // Plain text password - do direct comparison (temporary)
                    return password == hash;
                }
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsAdminAsync(string username)
        {
            var user = await GetUserAsync(username);
            return user != null && user.Role == "Admin";
        }
    }
}

