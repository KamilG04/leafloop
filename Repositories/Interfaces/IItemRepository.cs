using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models; // Dla Item, Photo
using LeafLoop.Services.DTOs; // Dla ItemSearchDto

namespace LeafLoop.Repositories.Interfaces
{
    // Zakładamy, że IRepository<T> jest zdefiniowany jak w Twoim przykładzie:
    // public interface IRepository<T> where T : class { ... metody GetByIdAsync, FindAsync, AddAsync, Remove, CountAsync(predicate) itd. ... }

    public interface IItemRepository : IRepository<Item> // Dziedziczy z IRepository<Item>
    {
        // --- Metody specyficzne dla Item ---

        Task<Item> GetItemWithDetailsAsync(int itemId);

        Task<IEnumerable<Item>> GetRecentItemsByUserWithCategoryAsync(int userId, int count);

        Task<IEnumerable<Item>> GetItemsByCategoryAsync(int categoryId);

        // Usunięto GetItemsByUserAsync(int userId) - można użyć FindAsync(i => i.UserId == userId) z IRepository<Item>

        Task<IEnumerable<Item>> GetAvailableItemsAsync(int count);

        Task<IEnumerable<Item>> GetItemsByTagAsync(int tagId);

        Task<IEnumerable<Photo>> GetItemPhotosAsync(int itemId);

        // --- Metody przyjmujące DTO (KLUCZOWE!) ---

        /// <summary>
        /// Searches for items based on the provided search criteria DTO.
        /// Implementacja powinna obsługiwać filtrowanie, sortowanie i paginację.
        /// </summary>
        Task<IEnumerable<Item>> SearchItemsAsync(ItemSearchDto searchDto);

        /// <summary>
        /// Counts items based on the provided search criteria DTO.
        /// Implementacja powinna stosować te same filtry co SearchItemsAsync.
        /// </summary>
        Task<int> CountAsync(ItemSearchDto searchDto);

        // --- Koniec metod specyficznych ---
    }
}