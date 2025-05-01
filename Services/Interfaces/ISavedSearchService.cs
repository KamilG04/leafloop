using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface ISavedSearchService
    {
        Task<SavedSearchDto> GetSavedSearchByIdAsync(int id);
        Task<IEnumerable<SavedSearchDto>> GetUserSavedSearchesAsync(int userId);
        Task<int> CreateSavedSearchAsync(SavedSearchCreateDto savedSearchDto);
        Task UpdateSavedSearchAsync(SavedSearchUpdateDto savedSearchDto, int userId);
        Task DeleteSavedSearchAsync(int id, int userId);
        Task<IEnumerable<ItemDto>> ExecuteSavedSearchAsync(int savedSearchId);
    }
}
