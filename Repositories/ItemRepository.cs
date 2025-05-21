// Path: LeafLoop/Repositories/ItemRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs; // For ItemSearchDto
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        public ItemRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<Item> GetItemWithDetailsAsync(int itemId)
        {
            return await _context.Items
                .Include(i => i.Photos) // Load all photos; ordering/selection for main photo handled by Mapper
                .Include(i => i.Category)
                .Include(i => i.User)
                .Include(i => i.Transactions)
                    .ThenInclude(t => t.Buyer)
                .Include(i => i.Tags)
                    .ThenInclude(it => it.Tag)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<IEnumerable<Item>> GetItemsByCategoryAsync(int categoryId)
        {
            return await _context.Items
                .Include(i => i.Photos) // Load photos
                .Include(i => i.Category) // This is redundant if filtering by CategoryId and Category is always present
                .Where(i => i.CategoryId == categoryId && i.IsAvailable)
                .OrderByDescending(i => i.DateAdded)
                .AsNoTracking()
                .ToListAsync();
        }

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
                .Include(i => i.Photos) // Load photos
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.DateAdded)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetAvailableItemsAsync(int count)
        {
             if (count <= 0) count = 12;
            return await _context.Items
                .Include(i => i.Photos) // Load photos
                .Include(i => i.Category)
                .Where(i => i.IsAvailable)
                .OrderByDescending(i => i.DateAdded)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetRecentItemsByUserWithCategoryAsync(int userId, int count)
        {
            if (count <= 0) count = 5;
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.User)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.DateAdded)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(ItemSearchDto searchDto)
        {
            var query = _context.Items.Where(i => i.IsAvailable);

            // Apply filtering (code from your previous version)
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
            // ... other filters from your SearchItemsAsync ...
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


            // Apply sorting (code from your previous version)
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
                default: 
                    query = descending ? query.OrderBy(i => i.DateAdded) : query.OrderByDescending(i => i.DateAdded);
                    break;
            }
            
             query = query.Include(i => i.Category)
                          .Include(i => i.Photos); // Load all photos for items in the page

            int page = searchDto.Page ?? 1;
            int pageSize = searchDto.PageSize ?? 8; 
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 8; 

            query = query.Skip((page - 1) * pageSize).Take(pageSize);

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<int> CountAsync(ItemSearchDto searchDto)
        {
            var query = _context.Items.Where(i => i.IsAvailable);

            // Apply the same filters as in SearchItemsAsync (code from your previous version)
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
            // ... other filters from your CountAsync ...
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

            return await query.CountAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsByTagAsync(int tagId)
        {
            return await _context.ItemTags
                .Where(it => it.TagId == tagId && it.Item.IsAvailable)
                .Include(it => it.Item)
                    .ThenInclude(i => i.Photos) // Load all photos
                .Include(it => it.Item)
                    .ThenInclude(i => i.Category) 
                .Select(it => it.Item) 
                .OrderByDescending(i => i.DateAdded) 
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Photo>> GetItemPhotosAsync(int itemId)
        {
            return await _context.Photos
                .Where(p => p.ItemId == itemId)
                .OrderBy(p => p.Id) // Or by an 'Order' property if it exists
                .AsNoTracking()
                .ToListAsync();
        }

        // --- Location-Based Search Methods Implementation ---

        public async Task<IEnumerable<Item>> GetItemsNearLocationAsync(
            decimal latitude, decimal longitude, decimal radiusKm,
            int? categoryId = null, string searchTerm = null,
            int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var latDegreeRadius = radiusKm / 111.0m;
            var lonDegreeRadius = radiusKm / (111.0m * (decimal)Math.Cos((double)latitude * Math.PI / 180.0));

            var minLat = latitude - latDegreeRadius;
            var maxLat = latitude + latDegreeRadius;
            var minLon = longitude - lonDegreeRadius;
            var maxLon = longitude + lonDegreeRadius;

            var query = _context.Items
                .Include(i => i.User)
                    .ThenInclude(u => u.Address)
                .Include(i => i.Category)
                .Include(i => i.Photos) // Load all photos
                .Where(i => i.IsAvailable &&
                             i.User != null && i.User.Address != null &&
                             i.User.Address.Latitude.HasValue && i.User.Address.Longitude.HasValue &&
                             i.User.Address.Latitude >= minLat && i.User.Address.Latitude <= maxLat &&
                             i.User.Address.Longitude >= minLon && i.User.Address.Longitude <= maxLon);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string termLower = searchTerm.ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(termLower) ||
                                         (i.Description != null && i.Description.ToLower().Contains(termLower)));
            }

            var itemsInBoundingBox = await query.AsNoTracking().ToListAsync();

            var filteredItems = itemsInBoundingBox.Where(item =>
            {
                if (!item.User.Address.Latitude.HasValue || !item.User.Address.Longitude.HasValue) return false;
                var itemLat = item.User.Address.Latitude.Value;
                var itemLon = item.User.Address.Longitude.Value;
                var dLat = ToRadians((double)(itemLat - latitude));
                var dLon = ToRadians((double)(itemLon - longitude));
                var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(ToRadians((double)latitude)) * Math.Cos(ToRadians((double)itemLat)) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                var distanceInKm = 6371 * c;
                return distanceInKm <= (double)radiusKm;
            })
            .OrderByDescending(i => i.DateAdded) 
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList(); 

            return filteredItems;
        }

        public async Task<int> CountItemsNearLocationAsync(
            decimal latitude, decimal longitude, decimal radiusKm,
            int? categoryId = null, string searchTerm = null)
        {
            var latDegreeRadius = radiusKm / 111.0m;
            var lonDegreeRadius = radiusKm / (111.0m * (decimal)Math.Cos((double)latitude * Math.PI / 180.0));

            var minLat = latitude - latDegreeRadius;
            var maxLat = latitude + latDegreeRadius;
            var minLon = longitude - lonDegreeRadius;
            var maxLon = longitude + lonDegreeRadius;

            var query = _context.Items
                .Include(i => i.User) // Include User to access Address
                    .ThenInclude(u => u.Address)
                .Where(i => i.IsAvailable &&
                             i.User != null && i.User.Address != null &&
                             i.User.Address.Latitude.HasValue && i.User.Address.Longitude.HasValue &&
                             i.User.Address.Latitude >= minLat && i.User.Address.Latitude <= maxLat &&
                             i.User.Address.Longitude >= minLon && i.User.Address.Longitude <= maxLon);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string termLower = searchTerm.ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(termLower) ||
                                         (i.Description != null && i.Description.ToLower().Contains(termLower)));
            }
            
            var itemsInBoundingBox = await query.AsNoTracking().ToListAsync();

            var count = itemsInBoundingBox.Count(item =>
            {
                if (!item.User.Address.Latitude.HasValue || !item.User.Address.Longitude.HasValue) return false;
                var itemLat = item.User.Address.Latitude.Value;
                var itemLon = item.User.Address.Longitude.Value;
                var dLat = ToRadians((double)(itemLat - latitude));
                var dLon = ToRadians((double)(itemLon - longitude));
                var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(ToRadians((double)latitude)) * Math.Cos(ToRadians((double)itemLat)) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                var distanceInKm = 6371 * c;
                return distanceInKm <= (double)radiusKm;
            });

            return count;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }
}