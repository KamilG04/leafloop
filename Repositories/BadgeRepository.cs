using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class BadgeRepository : Repository<Badge>, IBadgeRepository
    {
        public BadgeRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<User>> GetBadgeUsersAsync(int badgeId)
        {
            return await _context.UserBadges
                .Where(ub => ub.BadgeId == badgeId)
                .Include(ub => ub.User)
                .Select(ub => ub.User)
                .ToListAsync();
        }

        public async Task AssignBadgeToUserAsync(int badgeId, int userId)
        {
            var existingUserBadge = await _context.UserBadges
                .FirstOrDefaultAsync(ub => ub.BadgeId == badgeId && ub.UserId == userId);

            if (existingUserBadge == null)
            {
                var userBadge = new UserBadge
                {
                    BadgeId = badgeId,
                    UserId = userId,
                    AcquiredDate = DateTime.UtcNow
                };

                await _context.UserBadges.AddAsync(userBadge);
            }
        }
    }
}