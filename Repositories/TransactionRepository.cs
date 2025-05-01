using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<Transaction> GetTransactionWithDetailsAsync(int transactionId)
        {
            return await _context.Transactions
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Include(t => t.Item)
                    .ThenInclude(i => i.Photos)
                .Include(t => t.Messages)
                .Include(t => t.Ratings)
                .SingleOrDefaultAsync(t => t.Id == transactionId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByUserAsync(int userId, bool asSeller = false)
        {
            if (asSeller)
            {
                return await _context.Transactions
                    .Include(t => t.Buyer)
                    .Include(t => t.Item)
                        .ThenInclude(i => i.Photos)
                    .Where(t => t.SellerId == userId)
                    .OrderByDescending(t => t.StartDate)
                    .ToListAsync();
            }
            else
            {
                return await _context.Transactions
                    .Include(t => t.Seller)
                    .Include(t => t.Item)
                        .ThenInclude(i => i.Photos)
                    .Where(t => t.BuyerId == userId)
                    .OrderByDescending(t => t.StartDate)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByItemAsync(int itemId)
        {
            return await _context.Transactions
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Where(t => t.ItemId == itemId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status)
        {
            return await _context.Transactions
                .Include(t => t.Seller)
                .Include(t => t.Buyer)
                .Include(t => t.Item)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetTransactionMessagesAsync(int transactionId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.TransactionId == transactionId)
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }

        public async Task<double> GetUserRatingAverageAsync(int userId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.RatedEntityId == userId && r.RatedEntityType == RatedEntityType.User)
                .ToListAsync();

            if (ratings.Any())
            {
                return ratings.Average(r => r.Value);
            }

            return 0;
        }
    }
}
