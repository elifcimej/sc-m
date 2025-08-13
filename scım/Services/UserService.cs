using Microsoft.EntityFrameworkCore;
using scım.Data;
using scım.Models;

namespace scım.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<User?> GetUserByScimIdAsync(string scimId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.ScimId == scimId && u.IsActive);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.ScimId = Guid.NewGuid().ToString();
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User created: {Email}", user.Email);
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User updated: {Email}", user.Email);
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User deleted: {Email}", user.Email);
            return true;
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<bool> UserExistsByUserNameAsync(string userName)
        {
            return await _context.Users
                .AnyAsync(u => u.UserName == userName && u.IsActive);
        }
    }
}
