using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        public ItemRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<Item> GetItemWithDetailsAsync(int itemId)
        {
            return await _context.Items
                .Include(i => i.User)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Tags)
                    .ThenInclude(it => it.Tag)
                .SingleOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<IEnumerable<Item>> GetItemsByCategoryAsync(int categoryId)
        {
            return await _context.Items
                .Include(i => i.Photos)
                .Where(i => i.CategoryId == categoryId && i.IsAvailable)
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsByUserAsync(int userId)
        {
            return await _context.Items
                .Include(i => i.Photos)
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetAvailableItemsAsync(int count)
        {
            return await _context.Items
                .Include(i => i.Photos)
                .Include(i => i.Category)
                .Where(i => i.IsAvailable)
                .OrderByDescending(i => i.DateAdded)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm, int? categoryId = null)
        {
            var query = _context.Items
                .Include(i => i.Photos)
                .Include(i => i.Category)
                .Include(i => i.Tags)
                    .ThenInclude(it => it.Tag)
                .Where(i => i.IsAvailable);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i => i.Name.Contains(searchTerm) || 
                                         i.Description.Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            return await query
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsByTagAsync(int tagId)
        {
            return await _context.ItemTags
                .Where(it => it.TagId == tagId)
                .Include(it => it.Item)
                    .ThenInclude(i => i.Photos)
                .Include(it => it.Item)
                    .ThenInclude(i => i.Category)
                .Where(it => it.Item.IsAvailable)
                .Select(it => it.Item)
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();
        }

        public async Task<IEnumerable<Photo>> GetItemPhotosAsync(int itemId)
        {
            return await _context.Photos
                .Where(p => p.ItemId == itemId)
                .OrderBy(p => p.Id)
                .ToListAsync();
        }
    }
}