using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface ITagRepository : IRepository<Tag>
    {
        Task<IEnumerable<Tag>> GetItemTagsAsync(int itemId);
        Task<IEnumerable<Tag>> GetPopularTagsAsync(int count);
        Task AddTagToItemAsync(int itemId, int tagId);
        Task RemoveTagFromItemAsync(int itemId, int tagId);
    }
}