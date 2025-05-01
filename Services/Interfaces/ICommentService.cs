using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface ICommentService
    {
        Task<CommentDto> GetCommentByIdAsync(int id);
        Task<IEnumerable<CommentDto>> GetCommentsByContentAsync(int contentId, CommentContentType contentType);
        Task<IEnumerable<CommentDto>> GetCommentsByUserAsync(int userId);
        Task<int> AddCommentAsync(CommentCreateDto commentDto);
        Task UpdateCommentAsync(CommentUpdateDto commentDto, int userId);
        Task DeleteCommentAsync(int id, int userId);
    }
}
