// Path: LeafLoop/Services/ItemService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces; // Contains IUnitOfWork, IItemRepository
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.EntityFrameworkCore; // For CountAsync() if used directly on IQueryable
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class ItemService : IItemService
    {
        private readonly IUnitOfWork _unitOfWork; // IUnitOfWork type, .Items is of type IItemRepository
        private readonly IMapper _mapper;
        private readonly ILogger<ItemService> _logger;

        public ItemService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ItemService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ItemDto> GetItemByIdAsync(int id)
        {
            _logger.LogInformation("Attempting to get item with ID: {ItemId}", id);
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(id);
                if (item == null) {
                    _logger.LogWarning("Item with ID {ItemId} not found.", id);
                    return null;
                }
                return _mapper.Map<ItemDto>(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting item with ID: {ItemId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetItemsByUserAsync(int userId)
        {
            _logger.LogInformation("Attempting to get items for UserID: {UserId}", userId);
            try
            {
                // Option 1: Use the repository method (you need to ensure it exists and includes necessary relations for ItemDto)
                var items = await _unitOfWork.Items.GetItemsByUserWithRelationsAsync(userId);
            
                // Option 2: If you don't want a new repo method, use FindAsync but remember to include relations for mapping
                // var items = await _unitOfWork.Items.FindAsync(
                //    expression: i => i.UserId == userId,
                //    includes: new Expression<Func<Item, object>>[] { i => i.Category, i => i.Photos.OrderBy(p=>p.Id).Take(1) } // Example includes
                // );
            
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items by user: {UserId}", userId);
                throw;
            }
        }
        public async Task<IEnumerable<ItemDto>> GetRecentItemsByUserAsync(int userId, int count)
        {
            _logger.LogInformation("Attempting to get {Count} recent items for UserID: {UserId}", count, userId);
            try
            {
                var items = await _unitOfWork.Items.GetRecentItemsByUserWithCategoryAsync(userId, count);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {Count} recent items for user: {UserId}", count, userId);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> SearchItemsAsync(ItemSearchDto searchDto)
        {
            _logger.LogInformation("Attempting to search items with DTO: {@SearchDto}", searchDto);
            try
            {
                var items = await _unitOfWork.Items.SearchItemsAsync(searchDto);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching items with criteria: {@SearchDto}", searchDto);
                throw;
            }
        }

        public async Task<int> GetItemsCountAsync(ItemSearchDto searchDto)
        {
            _logger.LogInformation("Attempting to count items with DTO: {@SearchDto}", searchDto);
            try
            {
                 return await _unitOfWork.Items.CountAsync(searchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting items with criteria: {@SearchDto}", searchDto);
                throw;
            }
        }

        public async Task<ItemWithDetailsDto> GetItemWithDetailsAsync(int id)
        {
            _logger.LogInformation("Attempting to get item with details for ID: {ItemId}", id);
            try
            {
                var item = await _unitOfWork.Items.GetItemWithDetailsAsync(id);
                 if (item == null) {
                    _logger.LogWarning("Item with details for ID {ItemId} not found.", id);
                    return null;
                }
                return _mapper.Map<ItemWithDetailsDto>(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting item details with ID: {ItemId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetRecentItemsAsync(int count)
        {
            _logger.LogInformation("Attempting to get {Count} recent items.", count);
            if (count <= 0) count = 12;
            try
            {
                var items = await _unitOfWork.Items.GetAvailableItemsAsync(count);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting {Count} recent items", count);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetItemsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Attempting to get items for CategoryID: {CategoryId}, Page: {Page}, PageSize: {PageSize}", categoryId, page, pageSize);
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;
            try
            {
                // Repository method GetItemsByCategoryAsync needs to support pagination or fetch all and paginate here
                var items = await _unitOfWork.Items.GetItemsByCategoryAsync(categoryId); 
                // Example of in-memory pagination if repo doesn't do it (less efficient for large datasets)
                // var paginatedItems = items.Skip((page - 1) * pageSize).Take(pageSize);
                // Better: Modify GetItemsByCategoryAsync in repo to accept page/pageSize
                _logger.LogWarning("GetItemsByCategoryAsync in service currently fetches all items for category {CategoryId} then relies on client/further processing for pagination. Consider adding pagination to repository method.", categoryId);
                return _mapper.Map<IEnumerable<ItemDto>>(items); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items by category: {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<int> AddItemAsync(ItemCreateDto itemDto, int userId)
        {
            _logger.LogInformation("Attempting to add item by UserID: {UserId}, ItemName: {ItemName}", userId, itemDto.Name);
            try
            {
                var item = _mapper.Map<Item>(itemDto);
                item.UserId = userId;
                item.DateAdded = DateTime.UtcNow;
                item.IsAvailable = true; // Default to available on creation

                await _unitOfWork.Items.AddAsync(item);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully added item ID: {ItemId} by UserID: {UserId}", item.Id, userId);
                return item.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding item for UserID: {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateItemAsync(ItemUpdateDto itemDto, int userId)
        {
            _logger.LogInformation("Attempting to update item ID: {ItemId} by UserID: {UserId}", itemDto.Id, userId);
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(itemDto.Id);

                if (item == null)
                {
                    _logger.LogWarning("Item with ID {ItemId} not found for update by UserID: {UserId}", itemDto.Id, userId);
                    throw new KeyNotFoundException($"Item with ID {itemDto.Id} not found.");
                }

                if (item.UserId != userId)
                {
                    // Consider if Admins can edit - if so, add role check here
                    _logger.LogWarning("UserID {UserId} is not authorized to update item ID {ItemId} (owner is {OwnerId})", userId, item.Id, item.UserId);
                    throw new UnauthorizedAccessException("User is not authorized to update this item.");
                }

                _mapper.Map(itemDto, item);
                _unitOfWork.Items.Update(item); // Explicitly mark as updated
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully updated item ID: {ItemId} by UserID: {UserId}", item.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating item ID: {ItemId} by UserID: {UserId}", itemDto.Id, userId);
                throw;
            }
        }

        public async Task DeleteItemAsync(int id, int userId)
        {
            _logger.LogInformation("Attempting to delete item ID: {ItemId} by UserID: {UserId}", id, userId);
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(id);

                if (item == null)
                {
                    _logger.LogWarning("Item with ID {ItemId} not found for deletion by UserID: {UserId}", id, userId);
                    throw new KeyNotFoundException($"Item with ID {id} not found.");
                }

                if (item.UserId != userId)
                {
                     // Consider if Admins can delete
                    _logger.LogWarning("UserID {UserId} is not authorized to delete item ID {ItemId} (owner is {OwnerId})", userId, item.Id, item.UserId);
                    throw new UnauthorizedAccessException("User is not authorized to delete this item.");
                }

                _unitOfWork.Items.Remove(item);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully deleted item ID: {ItemId} by UserID: {UserId}", item.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting item ID: {ItemId} by UserID: {UserId}", id, userId);
                throw;
            }
        }

        public async Task<bool> IsItemOwnerAsync(int itemId, int userId)
        {
            // This check can be done without loading the full entity if performance is critical
            // e.g., await _unitOfWork.Items.AnyAsync(i => i.Id == itemId && i.UserId == userId);
            _logger.LogDebug("Checking item ownership for ItemId: {ItemId}, UserId: {UserId}", itemId, userId);
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(itemId);
                return item != null && item.UserId == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking item ownership. ItemId: {ItemId}, UserId: {UserId}", itemId, userId);
                return false; // Or re-throw depending on desired behavior
            }
        }

        public async Task<IEnumerable<PhotoDto>> GetItemPhotosAsync(int itemId)
        {
            _logger.LogInformation("Attempting to get photos for ItemId: {ItemId}", itemId);
            try
            {
                var photos = await _unitOfWork.Items.GetItemPhotosAsync(itemId);
                return _mapper.Map<IEnumerable<PhotoDto>>(photos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item photos for ItemId: {ItemId}", itemId);
                throw;
            }
        }

        public async Task AddTagToItemAsync(int itemId, int tagId, int userId)
        {
            _logger.LogInformation("Attempting to add TagId: {TagId} to ItemId: {ItemId} by UserID: {UserId}", tagId, itemId, userId);
            try
            {
                if (!await IsItemOwnerAsync(itemId, userId))
                {
                    _logger.LogWarning("UserID {UserId} is not authorized to modify tags for ItemID: {ItemId}", userId, itemId);
                    throw new UnauthorizedAccessException("User is not authorized to modify this item's tags.");
                }
                // Assuming IUnitOfWork.Tags has AddTagToItemAsync or similar method
                // This logic might be more complex and belong in a TagService or directly here if simple enough
                await _unitOfWork.Tags.AddTagToItemAsync(itemId, tagId); 
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully added TagId: {TagId} to ItemId: {ItemId}", tagId, itemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding TagId: {TagId} to ItemId: {ItemId}", tagId, itemId);
                throw;
            }
        }

        public async Task RemoveTagFromItemAsync(int itemId, int tagId, int userId)
        {
             _logger.LogInformation("Attempting to remove TagId: {TagId} from ItemId: {ItemId} by UserID: {UserId}", tagId, itemId, userId);
            try
            {
                if (!await IsItemOwnerAsync(itemId, userId))
                {
                    _logger.LogWarning("UserID {UserId} is not authorized to modify tags for ItemID: {ItemId}", userId, itemId);
                    throw new UnauthorizedAccessException("User is not authorized to modify this item's tags.");
                }
                await _unitOfWork.Tags.RemoveTagFromItemAsync(itemId, tagId);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully removed TagId: {TagId} from ItemId: {ItemId}", tagId, itemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing TagId: {TagId} from ItemId: {ItemId}", tagId, itemId);
                throw;
            }
        }

        public async Task MarkItemAsSoldAsync(int itemId, int userId)
        {
            _logger.LogInformation("Attempting to mark item ID: {ItemId} as sold by UserID: {UserId}", itemId, userId);
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(itemId);

                if (item == null)
                {
                    _logger.LogWarning("Item with ID {ItemId} not found to mark as sold by UserID: {UserId}", itemId, userId);
                    throw new KeyNotFoundException($"Item with ID {itemId} not found.");
                }

                if (item.UserId != userId)
                {
                    _logger.LogWarning("UserID {UserId} is not authorized to mark item ID {ItemId} as sold (owner is {OwnerId})", userId, item.Id, item.UserId);
                    throw new UnauthorizedAccessException("User is not authorized to mark this item as sold.");
                }

                if (!item.IsAvailable)
                {
                    _logger.LogInformation("Item ID {ItemId} is already marked as unavailable/sold.", itemId);
                    // Optionally throw an exception or return a specific status
                    return; 
                }

                item.IsAvailable = false;
                _unitOfWork.Items.Update(item);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully marked item ID: {ItemId} as sold by UserID: {UserId}", item.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking item ID: {ItemId} as sold by UserID: {UserId}", itemId, userId);
                throw;
            }
        }

        // --- NEW METHOD IMPLEMENTATION ---
        /// <inheritdoc/>
        public async Task<PagedResult<ItemSummaryDto>> GetItemsNearLocationAsync(
            decimal latitude,
            decimal longitude,
            decimal radiusKm,
            int? categoryId,
            string searchTerm,
            int pageNumber,
            int pageSize)
        {
            _logger.LogInformation("Service GetItemsNearLocationAsync: Lat={Lat}, Lon={Lon}, Radius={Radius}, Cat={Cat}, Term='{Term}', Page={Page}, Size={Size}",
                latitude, longitude, radiusKm, categoryId, searchTerm, pageNumber, pageSize);

            try
            {
                // 1. Get the raw list of items from the repository based on location and filters
                var itemsFromRepo = await _unitOfWork.Items.GetItemsNearLocationAsync(
                    latitude, longitude, radiusKm, categoryId, searchTerm, pageNumber, pageSize);

                // 2. Get the total count of items matching the criteria (for pagination)
                var totalCount = await _unitOfWork.Items.CountItemsNearLocationAsync(
                    latitude, longitude, radiusKm, categoryId, searchTerm);

                // 3. Map the Item entities to ItemSummaryDto
                // Ensure your AutoMapper profile correctly maps Item to ItemSummaryDto
                // This mapping might involve accessing Item.User.UserName, Item.User.Address.City,
                // and Item.Photos.FirstOrDefault().Path
                var itemSummaries = _mapper.Map<IEnumerable<ItemSummaryDto>>(itemsFromRepo);
                
                _logger.LogInformation("Service GetItemsNearLocationAsync: Mapped {ReturnedCount} items to ItemSummaryDto. Total potential items: {TotalCount}", itemSummaries.Count(), totalCount);

                return new PagedResult<ItemSummaryDto>(itemSummaries, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Service GetItemsNearLocationAsync: Lat={Lat}, Lon={Lon}, Radius={Radius}", latitude, longitude, radiusKm);
                throw; // Re-throw for the controller to handle
            }
        }
    }
}