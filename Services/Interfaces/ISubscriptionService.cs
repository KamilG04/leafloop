using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDto> GetSubscriptionByIdAsync(int id);
        Task<IEnumerable<SubscriptionDto>> GetUserSubscriptionsAsync(int userId);
        Task<int> SubscribeAsync(SubscriptionCreateDto subscriptionDto);
        Task UnsubscribeAsync(int id, int userId);
        Task<bool> IsUserSubscribedAsync(int userId, SubscriptionContentType contentType, int contentId);
    }
}
