using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<User> GetUserWithAddressAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Address)
                .SingleOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> GetUserWithItemsAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Items)
                .SingleOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> GetUserWithTransactionsAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.SellingTransactions)
                .Include(u => u.BuyingTransactions)
                .SingleOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetTopUsersByEcoScoreAsync(int count)
        {
            return await _context.Users
                .OrderByDescending(u => u.EcoScore)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Badge>> GetUserBadgesAsync(int userId)
        {
            var userBadges = await _context.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.Badge)
                .ToListAsync();

            return userBadges;
        }
    }
}