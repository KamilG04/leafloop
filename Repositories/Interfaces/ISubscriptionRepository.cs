using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(int userId);
    Task<IEnumerable<Subscription>> GetSubscriptionsByContentTypeAsync(SubscriptionContentType contentType);
}