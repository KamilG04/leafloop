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

        Task<IEnumerable<ItemDto>> GetRecentItemsByUserAsync(int userId, int count);



        /// <summary>
        /// Gets a paginated list of item summaries near a specified location.
        /// </summary>
        /// <param name="latitude">Latitude of the center point.</param>
        /// <param name="longitude">Longitude of the center point.</param>
        /// <param name="radiusKm">Search radius in kilometers.</param>
        /// <param name="categoryId">Optional category ID to filter by.</param>
        /// <param name="searchTerm">Optional search term.</param>
        /// <param name="pageNumber">Current page number (1-based).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>A PagedResult containing item summaries.</returns>
        Task<PagedResult<ItemSummaryDto>> GetItemsNearLocationAsync(
            decimal latitude,
            decimal longitude,
            decimal radiusKm,
            int? categoryId,
            string searchTerm,
            int pageNumber,
            int pageSize);
    }
}

