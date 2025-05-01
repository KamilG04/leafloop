using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class RatingRepository : Repository<Rating>, IRatingRepository
    {
        public RatingRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Rating>> GetRatingsByUserAsync(int userId, bool asRater = false)
        {
            if (asRater)
            {
                return await _context.Ratings
                    .Where(r => r.RaterId == userId)
                    .Include(r => r.Transaction)
                    .OrderByDescending(r => r.RatingDate)
                    .ToListAsync();
            }
            else
            {
                return await _context.Ratings
                    .Where(r => r.RatedEntityId == userId && r.RatedEntityType == RatedEntityType.User)
                    .Include(r => r.Rater)
                    .Include(r => r.Transaction)
                    .OrderByDescending(r => r.RatingDate)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<Rating>> GetRatingsByTransactionAsync(int transactionId)
        {
            return await _context.Ratings
                .Where(r => r.TransactionId == transactionId)
                .Include(r => r.Rater)
                .OrderByDescending(r => r.RatingDate)
                .ToListAsync();
        }

        // W Repositories/RatingRepository.cs
        public async Task<double> GetAverageRatingForEntityAsync(int entityId, RatedEntityType entityType)
        {
            // Oblicz średnią bezpośrednio w bazie danych
            double? average = await _context.Ratings
                .Where(r => r.RatedEntityId == entityId && r.RatedEntityType == entityType)
                // Rzutuj Value na double? aby AverageAsync zadziałało poprawnie i zwróciło null dla pustej kolekcji
                .AverageAsync(r => (double?)r.Value);

            // Zwróć średnią lub 0.0 jeśli nie było ocen (average jest null)
            return average ?? 0.0;
        }
    }
}
