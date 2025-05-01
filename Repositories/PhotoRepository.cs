using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class PhotoRepository : Repository<Photo>, IPhotoRepository
    {
        public PhotoRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Photo>> GetPhotosByItemAsync(int itemId)
        {
            return await _context.Photos
                .Where(p => p.ItemId == itemId)
                .OrderBy(p => p.Id)
                .ToListAsync();
        }
    }
}