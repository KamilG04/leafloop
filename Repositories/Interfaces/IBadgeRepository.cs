using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IBadgeRepository : IRepository<Badge>
    {
        Task<IEnumerable<User>> GetBadgeUsersAsync(int badgeId);
        Task AssignBadgeToUserAsync(int badgeId, int userId);
    }
}
