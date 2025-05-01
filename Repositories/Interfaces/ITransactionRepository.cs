using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<Transaction> GetTransactionWithDetailsAsync(int transactionId);
        Task<IEnumerable<Transaction>> GetTransactionsByUserAsync(int userId, bool asSeller = false);
        Task<IEnumerable<Transaction>> GetTransactionsByItemAsync(int itemId);
        Task<IEnumerable<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status);
        Task<IEnumerable<Message>> GetTransactionMessagesAsync(int transactionId);
        Task<double> GetUserRatingAverageAsync(int userId);
    }
}