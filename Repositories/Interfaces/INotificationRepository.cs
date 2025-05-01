using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
        Task<int> GetUnreadNotificationsCountAsync(int userId);
        Task MarkNotificationsAsReadAsync(int userId);
    }
}
