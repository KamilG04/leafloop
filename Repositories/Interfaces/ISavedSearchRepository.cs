using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces;

public interface ISavedSearchRepository : IRepository<SavedSearch>
{
    Task<IEnumerable<SavedSearch>> GetUserSavedSearchesAsync(int userId);
}