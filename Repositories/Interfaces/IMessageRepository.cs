using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetMessagesByUserAsync(int userId, bool asSender = false);
        Task<IEnumerable<Message>> GetConversationAsync(int user1Id, int user2Id);
        Task<IEnumerable<Message>> GetTransactionMessagesAsync(int transactionId);
        Task<int> GetUnreadMessagesCountAsync(int userId);
        Task MarkMessagesAsReadAsync(int recipientId, int senderId);
    }
}
