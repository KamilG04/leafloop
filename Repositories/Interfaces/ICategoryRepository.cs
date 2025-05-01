using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category> GetCategoryWithItemsAsync(int categoryId);
        Task<IEnumerable<Category>> GetPopularCategoriesAsync(int count);
    }
}