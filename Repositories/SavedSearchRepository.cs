using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class SavedSearchRepository : Repository<SavedSearch>, ISavedSearchRepository
    {
        public SavedSearchRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SavedSearch>> GetUserSavedSearchesAsync(int userId)
        {
            return await _context.SavedSearches
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }
    }
}