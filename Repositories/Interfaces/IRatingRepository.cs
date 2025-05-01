using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IRatingRepository : IRepository<Rating>
    {
        Task<IEnumerable<Rating>> GetRatingsByUserAsync(int userId, bool asRater = false);
        Task<IEnumerable<Rating>> GetRatingsByTransactionAsync(int transactionId);
        Task<double> GetAverageRatingForEntityAsync(int entityId, RatedEntityType entityType);
    }
}
