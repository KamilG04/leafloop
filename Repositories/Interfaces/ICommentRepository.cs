using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetCommentsByContentAsync(int contentId, CommentContentType contentType);
        Task<IEnumerable<Comment>> GetCommentsByUserAsync(int userId);
    }
}
