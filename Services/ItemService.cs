using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class ItemService : IItemService
    {
        private readonly IUnitOfWork _unitOfWork;
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
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(id);
                return _mapper.Map<ItemDto>(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting item with ID: {ItemId}", id);
                throw;
            }
        }

        public async Task<ItemWithDetailsDto> GetItemWithDetailsAsync(int id)
        {
            try
            {
                var item = await _unitOfWork.Items.GetItemWithDetailsAsync(id);
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
            try
            {
                var items = await _unitOfWork.Items.GetAvailableItemsAsync(count);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting recent items");
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetItemsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10)
        {
            try
            {
                var items = await _unitOfWork.Items.GetItemsByCategoryAsync(categoryId);
                
                // Apply pagination
                items = items.Skip((page - 1) * pageSize).Take(pageSize);
                
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items by category: {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetItemsByUserAsync(int userId)
        {
            try
            {
                var items = await _unitOfWork.Items.GetItemsByUserAsync(userId);
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items by user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> SearchItemsAsync(ItemSearchDto searchDto)
        {
            try
            {
                var items = await _unitOfWork.Items.SearchItemsAsync(searchDto.SearchTerm, searchDto.CategoryId);
                
                // Apply additional filters if needed
                if (searchDto.TagIds != null && searchDto.TagIds.Any())
                {
                    // This is a simplistic implementation for demonstration
                    // In a real app, you'd want to do this in the database query
                    var filteredItems = new List<Item>();
                    foreach (var item in items)
                    {
                        bool hasAllTags = true;
                        foreach (var tagId in searchDto.TagIds)
                        {
                            if (!item.Tags.Any(t => t.TagId == tagId))
                            {
                                hasAllTags = false;
                                break;
                            }
                        }
                        if (hasAllTags)
                        {
                            filteredItems.Add(item);
                        }
                    }
                    items = filteredItems;
                }
                
                // Apply pagination if specified
                if (searchDto.Page.HasValue && searchDto.PageSize.HasValue)
                {
                    items = items.Skip((searchDto.Page.Value - 1) * searchDto.PageSize.Value)
                                .Take(searchDto.PageSize.Value);
                }
                
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching items");
                throw;
            }
        }

        public async Task<int> AddItemAsync(ItemCreateDto itemDto, int userId)
        {
            try
            {
                var item = _mapper.Map<Item>(itemDto);
                item.UserId = userId;
                item.DateAdded = DateTime.UtcNow;
                item.IsAvailable = true;
                
                await _unitOfWork.Items.AddAsync(item);
                await _unitOfWork.CompleteAsync();
                
                return item.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding item");
                throw;
            }
        }

        public async Task UpdateItemAsync(ItemUpdateDto itemDto, int userId)
        {
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(itemDto.Id);
                
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item with ID {itemDto.Id} not found");
                }
                
                if (item.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to update this item");
                }
                
                _mapper.Map(itemDto, item);
                
                _unitOfWork.Items.Update(item);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating item with ID: {ItemId}", itemDto.Id);
                throw;
            }
        }

        public async Task DeleteItemAsync(int id, int userId)
        {
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(id);
                
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item with ID {id} not found");
                }
                
                if (item.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this item");
                }
                
                _unitOfWork.Items.Remove(item);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting item with ID: {ItemId}", id);
                throw;
            }
        }

        public async Task<bool> IsItemOwnerAsync(int itemId, int userId)
        {
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(itemId);
                
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item with ID {itemId} not found");
                }
                
                return item.UserId == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking item ownership. ItemId: {ItemId}, UserId: {UserId}", itemId, userId);
                throw;
            }
        }

        public async Task<IEnumerable<PhotoDto>> GetItemPhotosAsync(int itemId)
        {
            try
            {
                var photos = await _unitOfWork.Items.GetItemPhotosAsync(itemId);
                return _mapper.Map<IEnumerable<PhotoDto>>(photos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item photos. ItemId: {ItemId}", itemId);
                throw;
            }
        }

        public async Task AddTagToItemAsync(int itemId, int tagId, int userId)
        {
            try
            {
                // Verify item exists and user owns it
                if (!await IsItemOwnerAsync(itemId, userId))
                {
                    throw new UnauthorizedAccessException("User is not authorized to modify this item's tags");
                }
                
                await _unitOfWork.Tags.AddTagToItemAsync(itemId, tagId);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tag to item. ItemId: {ItemId}, TagId: {TagId}", itemId, tagId);
                throw;
            }
        }

        public async Task RemoveTagFromItemAsync(int itemId, int tagId, int userId)
        {
            try
            {
                // Verify item exists and user owns it
                if (!await IsItemOwnerAsync(itemId, userId))
                {
                    throw new UnauthorizedAccessException("User is not authorized to modify this item's tags");
                }
                
                await _unitOfWork.Tags.RemoveTagFromItemAsync(itemId, tagId);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag from item. ItemId: {ItemId}, TagId: {TagId}", itemId, tagId);
                throw;
            }
        }

        public async Task MarkItemAsSoldAsync(int itemId, int userId)
        {
            try
            {
                var item = await _unitOfWork.Items.GetByIdAsync(itemId);
                
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item with ID {itemId} not found");
                }
                
                if (item.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to mark this item as sold");
                }
                
                item.IsAvailable = false;
                
                _unitOfWork.Items.Update(item);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking item as sold. ItemId: {ItemId}", itemId);
                throw;
            }
        }
    }
}