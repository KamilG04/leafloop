using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionDto> GetTransactionByIdAsync(int id);
        Task<TransactionWithDetailsDto> GetTransactionWithDetailsAsync(int id);
        Task<IEnumerable<TransactionDto>> GetTransactionsByUserAsync(int userId, bool asSeller = false);
        Task<IEnumerable<TransactionDto>> GetTransactionsByItemAsync(int itemId);
        Task<IEnumerable<TransactionDto>> GetTransactionsByStatusAsync(TransactionStatus status);
        Task<int> InitiateTransactionAsync(TransactionCreateDto transactionDto, int buyerUserId); // Dodaj buyerUserId
        Task UpdateTransactionStatusAsync(int id, TransactionStatus status, int userId);
        Task CompleteTransactionAsync(int id, int userId);
        Task CancelTransactionAsync(int id, int userId);
        Task<bool> CanUserAccessTransactionAsync(int transactionId, int userId);
        Task ConfirmTransactionCompletionAsync(int id, int userId);
        Task CheckAndAutoCompleteTransactionsAsync();
    }
}
