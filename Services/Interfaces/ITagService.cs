using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface ITagService
    {
        Task<TagDto> GetTagByIdAsync(int id);
        Task<IEnumerable<TagDto>> GetAllTagsAsync();
        Task<IEnumerable<TagDto>> GetPopularTagsAsync(int count);
        Task<IEnumerable<TagDto>> GetItemTagsAsync(int itemId);
        Task<IEnumerable<ItemDto>> GetItemsByTagAsync(int tagId, int page = 1, int pageSize = 10);
        Task<int> CreateTagAsync(TagCreateDto tagDto);
        Task UpdateTagAsync(TagUpdateDto tagDto);
        Task DeleteTagAsync(int id);
    }
}
