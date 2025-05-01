using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(int userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionsByContentTypeAsync(SubscriptionContentType contentType)
        {
            return await _context.Subscriptions
                .Where(s => s.ContentType == contentType)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }
    }
}