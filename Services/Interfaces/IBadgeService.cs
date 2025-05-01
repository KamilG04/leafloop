using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IBadgeService
    {
        Task<BadgeDto> GetBadgeByIdAsync(int id);
        Task<IEnumerable<BadgeDto>> GetAllBadgesAsync();
        Task<IEnumerable<UserDto>> GetBadgeUsersAsync(int badgeId);
        Task<int> CreateBadgeAsync(BadgeCreateDto badgeDto);
        Task UpdateBadgeAsync(BadgeUpdateDto badgeDto);
        Task DeleteBadgeAsync(int id);
        Task AssignBadgeToUserAsync(int badgeId, int userId);
        Task RevokeBadgeFromUserAsync(int badgeId, int userId);
        Task<bool> CheckBadgeRequirementsAsync(int userId, int badgeId);
    }
}
