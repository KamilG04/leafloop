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
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CategoryService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                return _mapper.Map<CategoryDto>(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting category with ID: {CategoryId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetAllAsync();
                return _mapper.Map<IEnumerable<CategoryDto>>(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all categories");
                throw;
            }
        }

        public async Task<IEnumerable<CategoryDto>> GetPopularCategoriesAsync(int count)
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetPopularCategoriesAsync(count);
                return _mapper.Map<IEnumerable<CategoryDto>>(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting popular categories");
                throw;
            }
        }

        public async Task<IEnumerable<ItemDto>> GetItemsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetCategoryWithItemsAsync(categoryId);
                
                if (category == null)
                {
                    throw new KeyNotFoundException($"Category with ID {categoryId} not found");
                }
                
                var items = category.Items
                    .Where(i => i.IsAvailable)
                    .OrderByDescending(i => i.DateAdded)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);
                
                return _mapper.Map<IEnumerable<ItemDto>>(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting items for category: {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<int> CreateCategoryAsync(CategoryCreateDto categoryDto)
        {
            try
            {
                var category = _mapper.Map<Category>(categoryDto);
                
                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.CompleteAsync();
                
                return category.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category");
                throw;
            }
        }

        public async Task UpdateCategoryAsync(CategoryUpdateDto categoryDto)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(categoryDto.Id);
                
                if (category == null)
                {
                    throw new KeyNotFoundException($"Category with ID {categoryDto.Id} not found");
                }
                
                _mapper.Map(categoryDto, category);
                
                _unitOfWork.Categories.Update(category);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category: {CategoryId}", categoryDto.Id);
                throw;
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                
                if (category == null)
                {
                    throw new KeyNotFoundException($"Category with ID {id} not found");
                }
                
                // Check if category has items
                var categoryWithItems = await _unitOfWork.Categories.GetCategoryWithItemsAsync(id);
                if (categoryWithItems.Items.Any())
                {
                    throw new InvalidOperationException("Cannot delete category with associated items");
                }
                
                _unitOfWork.Categories.Remove(category);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category: {CategoryId}", id);
                throw;
            }
        }
    }
}