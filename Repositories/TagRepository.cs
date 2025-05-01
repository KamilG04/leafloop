using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class TagRepository : Repository<Tag>, ITagRepository
    {
        public TagRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Tag>> GetItemTagsAsync(int itemId)
        {
            return await _context.ItemTags
                .Where(it => it.ItemId == itemId)
                .Select(it => it.Tag)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count)
        {
            return await _context.Tags
                .OrderByDescending(t => t.Items.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task AddTagToItemAsync(int itemId, int tagId)
        {
            var existingItemTag = await _context.ItemTags
                .FirstOrDefaultAsync(it => it.ItemId == itemId && it.TagId == tagId);

            if (existingItemTag == null)
            {
                var itemTag = new ItemTag
                {
                    ItemId = itemId,
                    TagId = tagId
                };

                await _context.ItemTags.AddAsync(itemTag);
            }
        }

        public async Task RemoveTagFromItemAsync(int itemId, int tagId)
        {
            var itemTag = await _context.ItemTags
                .FirstOrDefaultAsync(it => it.ItemId == itemId && it.TagId == tagId);

            if (itemTag != null)
            {
                _context.ItemTags.Remove(itemTag);
            }
        }
    }
}