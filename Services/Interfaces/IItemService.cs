using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IItemService
    {
        Task<ItemDto> GetItemByIdAsync(int id);
        
        Task<ItemWithDetailsDto> GetItemWithDetailsAsync(int id);
        Task<IEnumerable<ItemDto>> GetRecentItemsAsync(int count);
        Task<int> GetItemsCountAsync(ItemSearchDto searchDto);
        Task<IEnumerable<ItemDto>> GetItemsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10);
        Task<IEnumerable<ItemDto>> GetItemsByUserAsync(int userId);
        Task<IEnumerable<ItemDto>> SearchItemsAsync(ItemSearchDto searchDto);
        Task<int> AddItemAsync(ItemCreateDto itemDto, int userId);
        Task UpdateItemAsync(ItemUpdateDto itemDto, int userId);
        Task DeleteItemAsync(int id, int userId);
        Task<bool> IsItemOwnerAsync(int itemId, int userId);
        Task<IEnumerable<PhotoDto>> GetItemPhotosAsync(int itemId);
        Task AddTagToItemAsync(int itemId, int tagId, int userId);
        Task RemoveTagFromItemAsync(int itemId, int tagId, int userId);
        Task MarkItemAsSoldAsync(int itemId, int userId);
    }
}
