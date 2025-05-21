using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models; // For Item, Photo
using LeafLoop.Services.DTOs; // For ItemSearchDto

namespace LeafLoop.Repositories.Interfaces
{
    // Assuming IRepository<T> is defined as in your example:
    // public interface IRepository<T> where T : class { ... methods GetByIdAsync, FindAsync, AddAsync, Remove, CountAsync(predicate) etc. ... }

    public interface IItemRepository : IRepository<Item> // Inherits from IRepository<Item>
    {
        // --- Item Specific Methods ---

        Task<Item> GetItemWithDetailsAsync(int itemId);

        Task<IEnumerable<Item>> GetRecentItemsByUserWithCategoryAsync(int userId, int count);

        Task<IEnumerable<Item>> GetItemsByCategoryAsync(int categoryId);

        // GetItemsByUserAsync(int userId) was removed - can use FindAsync(i => i.UserId == userId) from IRepository<Item>

        Task<IEnumerable<Item>> GetAvailableItemsAsync(int count);

        Task<IEnumerable<Item>> GetItemsByTagAsync(int tagId);

        Task<IEnumerable<Photo>> GetItemPhotosAsync(int itemId);
        
        Task<IEnumerable<Item>> GetItemsByUserWithRelationsAsync(int userId);

        // --- Methods accepting DTOs (KEY!) ---

        /// <summary>
        /// Searches for items based on the provided search criteria DTO.
        /// Implementation should handle filtering, sorting, and pagination.
        /// </summary>
        Task<IEnumerable<Item>> SearchItemsAsync(ItemSearchDto searchDto);

        /// <summary>
        /// Counts items based on the provided search criteria DTO.
        /// Implementation should apply the same filters as SearchItemsAsync.
        /// </summary>
        Task<int> CountAsync(ItemSearchDto searchDto); // Note: Naming this CountAsync might conflict if IRepository<T> has a generic CountAsync(). Consider CountItemsAsync(ItemSearchDto searchDto).

        // --- Location-Based Search Methods ---

        /// <summary>
        /// Gets a list of items near a given location with pagination and optional filters.
        /// </summary>
        /// <param name="latitude">Latitude of the center point.</param>
        /// <param name="longitude">Longitude of the center point.</param>
        /// <param name="radiusKm">Search radius in kilometers.</param>
        /// <param name="categoryId">Optional category ID to filter by.</param>
        /// <param name="searchTerm">Optional search term to filter by item name or description.</param>
        /// <param name="pageNumber">Page number for pagination (starting from 1).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>A list of items found within the specified radius from the center point.</returns>
        Task<IEnumerable<Item>> GetItemsNearLocationAsync(
            decimal latitude,
            decimal longitude,
            decimal radiusKm,
            int? categoryId = null,
            string searchTerm = null,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Gets the count of items near a given location with optional filters.
        /// </summary>
        /// <param name="latitude">Latitude of the center point.</param>
        /// <param name="longitude">Longitude of the center point.</param>
        /// <param name="radiusKm">Search radius in kilometers.</param>
        /// <param name="categoryId">Optional category ID to filter by.</param>
        /// <param name="searchTerm">Optional search term to filter by item name or description.</param>
        /// <returns>The total number of items found within the specified radius matching the criteria.</returns>
        Task<int> CountItemsNearLocationAsync(
            decimal latitude,
            decimal longitude,
            decimal radiusKm,
            int? categoryId = null,
            string searchTerm = null);

        // --- End of Item Specific Methods ---
    }
}