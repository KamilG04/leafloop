using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Comment>> GetCommentsByContentAsync(int contentId, CommentContentType contentType)
        {
            return await _context.Comments
                .Where(c => c.ContentId == contentId && c.ContentType == contentType)
                .Include(c => c.User)
                .OrderByDescending(c => c.AddedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetCommentsByUserAsync(int userId)
        {
            return await _context.Comments
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.AddedDate)
                .ToListAsync();
        }
    }
}