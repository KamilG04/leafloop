using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryDto> GetCategoryByIdAsync(int id);
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetPopularCategoriesAsync(int count);
        Task<IEnumerable<ItemDto>> GetItemsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10);
        Task<int> CreateCategoryAsync(CategoryCreateDto categoryDto);
        Task UpdateCategoryAsync(CategoryUpdateDto categoryDto);
        Task DeleteCategoryAsync(int id);
    }
}
