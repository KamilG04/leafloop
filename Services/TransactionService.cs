using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TransactionService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<TransactionDto> GetTransactionByIdAsync(int id)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
                return _mapper.Map<TransactionDto>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transaction with ID: {TransactionId}", id);
                throw;
            }
        }

        public async Task<TransactionWithDetailsDto> GetTransactionWithDetailsAsync(int id)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetTransactionWithDetailsAsync(id);
                
                if (transaction == null)
                {
                    return null;
                }
                
                return _mapper.Map<TransactionWithDetailsDto>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transaction details for ID: {TransactionId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsByUserAsync(int userId, bool asSeller = false)
        {
            try
            {
                var transactions = await _unitOfWork.Transactions.GetTransactionsByUserAsync(userId, asSeller);
                return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transactions for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsByItemAsync(int itemId)
        {
            try
            {
                var transactions = await _unitOfWork.Transactions.GetTransactionsByItemAsync(itemId);
                return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transactions for item: {ItemId}", itemId);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsByStatusAsync(TransactionStatus status)
        {
            try
            {
                var transactions = await _unitOfWork.Transactions.GetTransactionsByStatusAsync(status);
                return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transactions by status: {Status}", status);
                throw;
            }
        }

        public async Task<int> InitiateTransactionAsync(TransactionCreateDto transactionDto, int buyerUserId)
        
        {
            try
            {
                // First check if the item exists and is available
                var item = await _unitOfWork.Items.GetByIdAsync(transactionDto.ItemId);
                
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item with ID {transactionDto.ItemId} not found");
                }
                
                if (!item.IsAvailable)
                {
                    throw new InvalidOperationException($"Item with ID {transactionDto.ItemId} is not available for transaction");
                }
                
                // Create the transaction
                var transaction = _mapper.Map<Transaction>(transactionDto);
                transaction.SellerId = item.UserId; // Set seller ID from item's owner
                transaction.StartDate = DateTime.UtcNow;
                transaction.Status = TransactionStatus.Pending;
                
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.CompleteAsync();
                
                // If there's an initial message, create it
                if (!string.IsNullOrEmpty(transactionDto.InitialMessage))
                {
                    var message = new Message
                    {
                        Content = transactionDto.InitialMessage,
                        SenderId = transaction.BuyerId,
                        ReceiverId = transaction.SellerId,
                        TransactionId = transaction.Id,
                        SentDate = DateTime.UtcNow,
                        IsRead = false
                    };
                    
                    await _unitOfWork.Messages.AddAsync(message);
                    await _unitOfWork.CompleteAsync();
                }
                
                return transaction.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initiating transaction");
                throw;
            }
            var existingTransaction = await _unitOfWork.Transactions.SingleOrDefaultAsync(t => 
                t.ItemId == transactionDto.ItemId && 
                t.BuyerId == buyerUserId && 
                (t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.InProgress));
            if (existingTransaction != null)
            {
                throw new InvalidOperationException("You already have a pending transaction for this item");
            }
        }

        public async Task UpdateTransactionStatusAsync(int id, TransactionStatus status, int userId)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
                
                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Transaction with ID {id} not found");
                }
                
                // Verify that the user is either the buyer or seller
                if (transaction.BuyerId != userId && transaction.SellerId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to update this transaction");
                }
                
                // Check allowed status transitions
                if (!IsValidStatusTransition(transaction.Status, status))
                {
                    throw new InvalidOperationException($"Invalid status transition from {transaction.Status} to {status}");
                }
                
                transaction.Status = status;
                
                // If completing or cancelling the transaction, set the end date
                if (status == TransactionStatus.Completed || status == TransactionStatus.Cancelled)
                {
                    transaction.EndDate = DateTime.UtcNow;
                }
                
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating transaction status: {TransactionId}", id);
                throw;
            }
        }

        public async Task CompleteTransactionAsync(int id, int userId)
        {
            try
            {
                // Update status to Completed
                await UpdateTransactionStatusAsync(id, TransactionStatus.Completed, userId);
                
                // Mark item as not available
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
                var item = await _unitOfWork.Items.GetByIdAsync(transaction.ItemId);
                
                if (item != null && item.IsAvailable)
                {
                    item.IsAvailable = false;
                    _unitOfWork.Items.Update(item);
                    await _unitOfWork.CompleteAsync();
                }
                
                // TODO: Update eco scores for both users
                await UpdateUserEcoScoresAsync(transaction.SellerId, transaction.BuyerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while completing transaction: {TransactionId}", id);
                throw;
            }
        }

        public async Task CancelTransactionAsync(int id, int userId)
        {
            try
            {
                // Update status to Cancelled
                await UpdateTransactionStatusAsync(id, TransactionStatus.Cancelled, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling transaction: {TransactionId}", id);
                throw;
            }
        }

        public async Task<bool> CanUserAccessTransactionAsync(int transactionId, int userId)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                
                if (transaction == null)
                {
                    return false;
                }
                
                return transaction.BuyerId == userId || transaction.SellerId == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can access transaction. TransactionId: {TransactionId}, UserId: {UserId}", transactionId, userId);
                return false;
            }
        }

        #region Private Helpers

        private bool IsValidStatusTransition(TransactionStatus currentStatus, TransactionStatus newStatus)
        {
            switch (currentStatus)
            {
                case TransactionStatus.Pending:
                    return newStatus == TransactionStatus.InProgress || newStatus == TransactionStatus.Cancelled;
                
                case TransactionStatus.InProgress:
                    return newStatus == TransactionStatus.Completed || newStatus == TransactionStatus.Cancelled;
                
                case TransactionStatus.Completed:
                case TransactionStatus.Cancelled:
                    return false; // Final states cannot be changed
                
                default:
                    return false;
            }
        }

        private async Task UpdateUserEcoScoresAsync(int sellerId, int buyerId)
        {
            try
            {
                // Get the users
                var seller = await _unitOfWork.Users.GetByIdAsync(sellerId);
                var buyer = await _unitOfWork.Users.GetByIdAsync(buyerId);
                
                if (seller != null && buyer != null)
                {
                    // Award eco points for the transaction
                    seller.EcoScore += 5; // Example: 5 points for selling an item
                    buyer.EcoScore += 3;  // Example: 3 points for buying a second-hand item
                    
                    _unitOfWork.Users.Update(seller);
                    _unitOfWork.Users.Update(buyer);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating eco scores for seller {SellerId} and buyer {BuyerId}", sellerId, buyerId);
                // Don't throw, as this is a non-critical operation
            }
        }

        #endregion
    }
}