using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IItemRepository : IRepository<Item>
    {
        Task<Item> GetItemWithDetailsAsync(int itemId);
        Task<IEnumerable<Item>> GetItemsByCategoryAsync(int categoryId);
        Task<IEnumerable<Item>> GetItemsByUserAsync(int userId);
        Task<IEnumerable<Item>> GetAvailableItemsAsync(int count);
        Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm, int? categoryId = null);
        Task<IEnumerable<Item>> GetItemsByTagAsync(int tagId);
        Task<IEnumerable<Photo>> GetItemPhotosAsync(int itemId);
    }
}