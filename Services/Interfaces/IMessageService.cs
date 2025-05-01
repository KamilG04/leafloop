using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IMessageService
    {
        Task<MessageDto> GetMessageByIdAsync(int id);
        Task<IEnumerable<MessageDto>> GetMessagesByUserAsync(int userId, bool asSender = false);
        Task<IEnumerable<MessageDto>> GetConversationAsync(int user1Id, int user2Id);
        Task<IEnumerable<MessageDto>> GetTransactionMessagesAsync(int transactionId);
        Task<int> SendMessageAsync(MessageCreateDto messageDto);
        Task MarkMessageAsReadAsync(int id, int userId);
        Task MarkAllMessagesAsReadAsync(int receiverId, int senderId);
        Task<int> GetUnreadMessagesCountAsync(int userId);
    }
}
