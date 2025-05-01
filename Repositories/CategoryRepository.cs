using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<Category> GetCategoryWithItemsAsync(int categoryId)
        {
            return await _context.Categories
                .Include(c => c.Items)
                .SingleOrDefaultAsync(c => c.Id == categoryId);
        }

        public async Task<IEnumerable<Category>> GetPopularCategoriesAsync(int count)
        {
            return await _context.Categories
                .OrderByDescending(c => c.Items.Count)
                .Take(count)
                .ToListAsync();
        }
    }
}