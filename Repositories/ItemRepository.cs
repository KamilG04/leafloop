using System;
using System.Collections.Generic;
using System.Linq; // Potrzebne dla LINQ
using System.Threading.Tasks;
using LeafLoop.Data; // Dla DbContext
using LeafLoop.Models; // Dla Item, Photo, ItemTag itp.
using LeafLoop.Repositories.Interfaces; // Dla IItemRepository
using LeafLoop.Services.DTOs; // Dla ItemSearchDto
using Microsoft.EntityFrameworkCore; // Dla metod EF Core (Include, ToListAsync, CountAsync itp.)

namespace LeafLoop.Repositories
{
    // Zakładamy, że istnieje bazowa klasa Repository<T>
    // Jeśli nie, ItemRepository musi implementować wszystkie metody z IRepository<Item>
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        // Dziedziczymy _context z klasy bazowej Repository<Item>
        // Jeśli nie ma klasy bazowej, dodaj pole: protected readonly LeafLoopDbContext _context;
        public ItemRepository(LeafLoopDbContext context) : base(context) // Przekaż context do bazy
        {
            // Jeśli nie ma klasy bazowej, przypisz: _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets an item with its details (User, Category, Photos, Tags).
        /// Uses AsNoTracking for read-only query optimization.
        /// </summary>
        public async Task<Item> GetItemWithDetailsAsync(int itemId)
        {
            // THE KEY FIX IS HERE: Use .Include()
            return await _context.Items
                .Include(i => i.Photos)       // <-- LOAD THE PHOTOS!
                .Include(i => i.Category)     // Load related Category
                .Include(i => i.User)  
                .Include(i => i.Transactions)
                .ThenInclude(t => t.Buyer)
                // Load related User
                // .Include(i => i.Tags)      // Uncomment if ItemWithDetailsDto needs tags
                // .AsNoTracking()            // Optional: Good for read-only queries
                .FirstOrDefaultAsync(i => i.Id == itemId); // Find the specific item
                
        }

        /// <summary>
        /// Gets available items for a specific category, ordered by date, including the first photo.
        /// </summary>
        public async Task<IEnumerable<Item>> GetItemsByCategoryAsync(int categoryId)
        {
            return await _context.Items
                .Include(i => i.Photos.OrderBy(p => p.Id).Take(1)) // Tylko pierwsze zdjęcie (miniatura)
                .Where(i => i.CategoryId == categoryId && i.IsAvailable) // Filtruj po kategorii i dostępności
                .OrderByDescending(i => i.DateAdded) // Sortuj od najnowszych
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Gets all items for a specific user, ordered by date, including category and the first photo.
        /// </summary>
        public async Task<IEnumerable<Item>> GetItemsByUserWithRelationsAsync(int userId)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.User)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.DateAdded)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<Item>> GetItemsByUserAsync(int userId)
        {
            return await _context.Items
                .Include(i => i.Photos.OrderBy(p => p.Id).Take(1)) // Tylko pierwsze zdjęcie
                .Include(i => i.Category) // Dołącz kategorię
                .Where(i => i.UserId == userId) // Filtruj po użytkowniku
                .OrderByDescending(i => i.DateAdded) // Sortuj od najnowszych
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Gets a specified number of the most recent available items, including category and the first photo.
        /// </summary>
        public async Task<IEnumerable<Item>> GetAvailableItemsAsync(int count)
        {
             if (count <= 0) count = 12; // Zabezpieczenie/domyślny limit
            return await _context.Items
                .Include(i => i.Photos.OrderBy(p => p.Id).Take(1)) // Tylko pierwsze zdjęcie
                .Include(i => i.Category)
                .Where(i => i.IsAvailable) // Tylko dostępne
                .OrderByDescending(i => i.DateAdded) // Najnowsze
                .Take(count) // Limit wyników
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Gets a specified number of the most recent items listed by a specific user,
        /// including their category and first photo information.
        /// </summary>
        public async Task<IEnumerable<Item>> GetRecentItemsByUserWithCategoryAsync(int userId, int count)
        {
           
                if (count <= 0) count = 5;
    
                return await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.Photos)  // Załaduj wszystkie zdjęcia
                    .Include(i => i.User)    // Dołącz User
                    .Where(i => i.UserId == userId)
                    .OrderByDescending(i => i.DateAdded)
                    .Take(count)
                    .AsNoTracking()
                    .ToListAsync();
                                   // Wykonaj zapytanie
        }       

        /// <summary>
        /// Searches for available items based on various criteria provided in the search DTO,
        /// including filtering, sorting, and pagination. Includes category and first photo.
        /// </summary>
        public async Task<IEnumerable<Item>> SearchItemsAsync(ItemSearchDto searchDto)
        {
            // Rozpocznij od podstawowego zapytania IQueryable dla dostępnych przedmiotów
            var query = _context.Items.Where(i => i.IsAvailable);

            // Zastosuj filtrowanie na podstawie DTO
            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                string term = searchDto.SearchTerm.ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(term) ||
                                         (i.Description != null && i.Description.ToLower().Contains(term))); // Sprawdź null dla Description
            }
            if (searchDto.CategoryId.HasValue && searchDto.CategoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == searchDto.CategoryId.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchDto.Condition))
            {
                query = query.Where(i => i.Condition == searchDto.Condition);
            }
            if (searchDto.IsForExchange.HasValue)
            {
                 query = query.Where(i => i.IsForExchange == searchDto.IsForExchange.Value);
            }
            if (searchDto.MinValue.HasValue)
            {
                query = query.Where(i => i.ExpectedValue >= searchDto.MinValue.Value);
            }
            if (searchDto.MaxValue.HasValue)
            {
                 query = query.Where(i => i.ExpectedValue <= searchDto.MaxValue.Value);
            }
             if (searchDto.AddedAfter.HasValue)
            {
                 query = query.Where(i => i.DateAdded >= searchDto.AddedAfter.Value);
            }
            // Filtrowanie po TagIds jest bardziej złożone i wymaga JOIN z ItemTags
            if (searchDto.TagIds != null && searchDto.TagIds.Any())
            {
                // Dla każdego tagu, przedmiot musi mieć powiązanie w ItemTags
                foreach (var tagId in searchDto.TagIds)
                {
                    query = query.Where(i => i.Tags.Any(it => it.TagId == tagId));
                }
            }

            // Zastosuj sortowanie
            bool descending = searchDto.SortDescending;
            switch (searchDto.SortBy?.ToLowerInvariant())
            {
                case "name":
                    query = descending ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name);
                    break;
                case "value":
                     query = descending ? query.OrderByDescending(i => i.ExpectedValue) : query.OrderBy(i => i.ExpectedValue);
                     break;
                case "date":
                default: // Domyślne sortowanie po dacie dodania (najnowsze pierwsze)
                    query = descending ? query.OrderBy(i => i.DateAdded) : query.OrderByDescending(i => i.DateAdded);
                    break;
            }

             // Dołącz powiązane dane (po filtrowaniu i sortowaniu, przed paginacją)
             query = query.Include(i => i.Category)
                          .Include(i => i.Photos.OrderBy(p => p.Id).Take(1)); // Tylko pierwsze zdjęcie

            // Zastosuj paginację
            int page = searchDto.Page ?? 1;
            int pageSize = searchDto.PageSize ?? 8; // Domyślny rozmiar strony
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 8; // Limit rozmiaru strony

            query = query.Skip((page - 1) * pageSize).Take(pageSize);

            // Wykonaj zapytanie i zwróć wyniki
            return await query.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Counts available items based on the provided search criteria.
        /// </summary>
        public async Task<int> CountAsync(ItemSearchDto searchDto)
        {
            var query = _context.Items.Where(i => i.IsAvailable);

            // Zastosuj te same filtry co w SearchItemsAsync
            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                string term = searchDto.SearchTerm.ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(term) ||
                                         (i.Description != null && i.Description.ToLower().Contains(term)));
            }
            if (searchDto.CategoryId.HasValue && searchDto.CategoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == searchDto.CategoryId.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchDto.Condition))
            {
                query = query.Where(i => i.Condition == searchDto.Condition);
            }
            if (searchDto.IsForExchange.HasValue)
            {
                 query = query.Where(i => i.IsForExchange == searchDto.IsForExchange.Value);
            }
             if (searchDto.MinValue.HasValue)
            {
                query = query.Where(i => i.ExpectedValue >= searchDto.MinValue.Value);
            }
            if (searchDto.MaxValue.HasValue)
            {
                 query = query.Where(i => i.ExpectedValue <= searchDto.MaxValue.Value);
            }
             if (searchDto.AddedAfter.HasValue)
            {
                 query = query.Where(i => i.DateAdded >= searchDto.AddedAfter.Value);
            }
             if (searchDto.TagIds != null && searchDto.TagIds.Any())
            {
                foreach (var tagId in searchDto.TagIds)
                {
                    query = query.Where(i => i.Tags.Any(it => it.TagId == tagId));
                }
            }

            // Zlicz pasujące elementy
            return await query.CountAsync();
        }


        public async Task<IEnumerable<Item>> GetItemsByTagAsync(int tagId)
        {
            // Zakładamy istnienie encji łączącej ItemTag i DbSet ItemTags
            return await _context.ItemTags
                .Where(it => it.TagId == tagId)
                .Include(it => it.Item)
                    .ThenInclude(i => i.Photos.OrderBy(p => p.Id).Take(1))
                .Include(it => it.Item)
                    .ThenInclude(i => i.Category)
                .Where(it => it.Item.IsAvailable)
                .Select(it => it.Item)
                .OrderByDescending(i => i.DateAdded)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Photo>> GetItemPhotosAsync(int itemId)
        {
             // Zakładamy istnienie DbSet Photos
            return await _context.Photos
                .Where(p => p.ItemId == itemId)
                .OrderBy(p => p.Id) // Można dodać sortowanie, np. po kolejności
                .AsNoTracking()
                .ToListAsync();
        }

        // Metody AddTagToItemAsync i RemoveTagFromItemAsync powinny być w ITagRepository/TagRepository
        // Jeśli muszą być tutaj, to implementacja zależy od struktury ItemTag
    }
}
