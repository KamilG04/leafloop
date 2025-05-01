using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        public MessageRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Message>> GetMessagesByUserAsync(int userId, bool asSender = false)
        {
            if (asSender)
            {
                return await _context.Messages
                    .Where(m => m.SenderId == userId)
                    .Include(m => m.Receiver)
                    .OrderByDescending(m => m.SentDate)
                    .ToListAsync();
            }
            else
            {
                return await _context.Messages
                    .Where(m => m.ReceiverId == userId)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.SentDate)
                    .ToListAsync();
            }
        }

        public async Task<IEnumerable<Message>> GetConversationAsync(int user1Id, int user2Id)
        {
            return await _context.Messages
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                            (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetTransactionMessagesAsync(int transactionId)
        {
            return await _context.Messages
                .Where(m => m.TransactionId == transactionId)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }

        public async Task<int> GetUnreadMessagesCountAsync(int userId)
        {
            return await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
        }

        public async Task MarkMessagesAsReadAsync(int recipientId, int senderId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.ReceiverId == recipientId && m.SenderId == senderId && !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
        }
    }
}
