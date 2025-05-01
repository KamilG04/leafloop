using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserWithAddressAsync(int userId);
        Task<User> GetUserWithItemsAsync(int userId);
        Task<User> GetUserWithTransactionsAsync(int userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetTopUsersByEcoScoreAsync(int count);
        Task<IEnumerable<Badge>> GetUserBadgesAsync(int userId);
    }
}