using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> GetNotificationByIdAsync(int id);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
        Task<int> GetUnreadNotificationsCountAsync(int userId);
        Task<int> CreateNotificationAsync(NotificationCreateDto notificationDto);
        Task CreateSystemNotificationAsync(string type, string content, IEnumerable<int> userIds);
        Task MarkNotificationAsReadAsync(int id, int userId);
        Task MarkAllNotificationsAsReadAsync(int userId);
    }
}
