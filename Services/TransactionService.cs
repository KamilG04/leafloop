using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogInformation("Getting transaction by ID: {TransactionId}", id);
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with ID: {TransactionId} not found in GetTransactionByIdAsync.", id);
                    return null;
                }

                return _mapper.Map<TransactionDto>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transaction with ID: {TransactionId}", id);
                throw;
            }
        }
        
        // Metoda GetTransactionWithDetailsAsync (bez zmian)
        public async Task<TransactionWithDetailsDto> GetTransactionWithDetailsAsync(int id)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetTransactionWithDetailsAsync(id);
                if (transaction == null) return null;
                return _mapper.Map<TransactionWithDetailsDto>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transaction details for ID: {TransactionId}", id);
                throw;
            }
        }

        // Metoda GetTransactionsByUserAsync (bez zmian)
        public async Task<IEnumerable<TransactionDto>> GetTransactionsByUserAsync(int userId, bool asSeller = false)
        {
            try
            {
                // Zakładamy, że repozytorium zwraca transakcje z załadowanymi danymi potrzebnymi dla TransactionDto
                var transactions = await _unitOfWork.Transactions.GetTransactionsByUserAsync(userId, asSeller);
                return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transactions for user: {UserId}, AsSeller: {AsSeller}", userId, asSeller);
                throw;
            }
        }

        // Metoda GetTransactionsByItemAsync (bez zmian)
        public async Task<IEnumerable<TransactionDto>> GetTransactionsByItemAsync(int itemId)
        {
            try
            {
                // Zakładamy, że repozytorium zwraca transakcje z załadowanymi danymi potrzebnymi dla TransactionDto
                var transactions = await _unitOfWork.Transactions.GetTransactionsByItemAsync(itemId);
                return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transactions for item: {ItemId}", itemId);
                throw;
            }
        }

        // Metoda GetTransactionsByStatusAsync (bez zmian)
        public async Task<IEnumerable<TransactionDto>> GetTransactionsByStatusAsync(TransactionStatus status)
        {
            try
            {
                // Zakładamy, że repozytorium zwraca transakcje z załadowanymi danymi potrzebnymi dla TransactionDto
                var transactions = await _unitOfWork.Transactions.GetTransactionsByStatusAsync(status);
                return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting transactions by status: {Status}", status);
                throw;
            }
        }

        // POPRAWIONA METODA InitiateTransactionAsync
        public async Task<int> InitiateTransactionAsync(TransactionCreateDto transactionDto, int buyerUserId)
        {
            _logger.LogInformation("Attempting to initiate transaction for ItemId {ItemId} of Type {TransactionType} by UserID {BuyerId}", 
                transactionDto.ItemId, transactionDto.Type, buyerUserId);
            
            try
            {
                // Use transaction to ensure atomicity
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Get the item with locking hint
                    var item = await _unitOfWork.Items.GetByIdAsync(transactionDto.ItemId);
                    if (item == null)
                        throw new KeyNotFoundException($"Item with ID {transactionDto.ItemId} not found.");

                    if (!item.IsAvailable)
                        throw new InvalidOperationException($"Item with ID {transactionDto.ItemId} is not available for transaction.");

                    if (item.UserId == buyerUserId)
                        throw new InvalidOperationException("You cannot initiate a transaction for your own item.");

                    // 2. Check existing transactions
                    var existingTransaction = await _unitOfWork.Transactions.SingleOrDefaultAsync(t =>
                        t.ItemId == transactionDto.ItemId &&
                        t.BuyerId == buyerUserId &&
                        (t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.InProgress)
                    );

                    if (existingTransaction != null)
                        throw new InvalidOperationException($"You already have a pending or in-progress transaction (ID: {existingTransaction.Id}) for this item.");

                    // 3. Create transaction
                    var transaction = new Transaction
                    {
                        ItemId = transactionDto.ItemId,
                        BuyerId = buyerUserId,
                        SellerId = item.UserId,
                        StartDate = DateTime.UtcNow,
                        LastUpdateDate = DateTime.UtcNow,
                        Status = TransactionStatus.Pending,
                        Type = transactionDto.Type,
                        BuyerConfirmed = false,
                        SellerConfirmed = false
                    };

                    // 4. Update item availability (optimistic concurrency)
                    item.IsAvailable = false;

                    // 5. Save both changes together in the transaction
                    await _unitOfWork.Transactions.AddAsync(transaction);
                    await _unitOfWork.CompleteAsync();

                    _logger.LogInformation("Transaction initiated successfully. TransactionID: {TransactionId}", transaction.Id);
                    return transaction.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initiating transaction for ItemId {ItemId} by UserID {BuyerId}", 
                    transactionDto.ItemId, buyerUserId);
                throw;
            }
        }

        // POPRAWIONA METODA UpdateTransactionStatusAsync
        public async Task UpdateTransactionStatusAsync(int id, TransactionStatus status, int userId)
        {
            _logger.LogInformation("Attempting to update status for TransactionID: {TransactionId} to {NewStatus} by UserID: {UserId}", id, status, userId);
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
                if (transaction == null)
                    throw new KeyNotFoundException($"Transaction with ID {id} not found");

                if (transaction.BuyerId != userId && transaction.SellerId != userId)
                    throw new UnauthorizedAccessException("User is not authorized to update this transaction");

                if (!IsValidStatusTransition(transaction.Status, status))
                    throw new InvalidOperationException($"Invalid status transition from {transaction.Status} to {status}");

                transaction.Status = status;
                transaction.LastUpdateDate = DateTime.UtcNow;

                if (status == TransactionStatus.Completed || status == TransactionStatus.Cancelled)
                    transaction.EndDate = DateTime.UtcNow;
                else
                    transaction.EndDate = null; // Wyczyść EndDate, jeśli status wraca do nie-końcowego (mało prawdopodobne)

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Successfully updated status for TransactionID: {TransactionId} to {NewStatus}", id, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating transaction status: {TransactionId}, NewStatus: {NewStatus}, UserID: {UserId}", 
                    id, status, userId);
                throw;
            }
        }

        // Metoda CompleteTransactionAsync (bez zmian logiki)
        public async Task CompleteTransactionAsync(int id, int userId)
        {
            try
            {
                // Update status to Completed
                await UpdateTransactionStatusAsync(id, TransactionStatus.Completed, userId);

                // Mark item as not available
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} not found after status update during CompleteTransactionAsync.", id);
                    // Można rzucić wyjątek lub po prostu zakończyć
                    throw new KeyNotFoundException($"Transaction with ID {id} not found after status update.");
                }

                var item = await _unitOfWork.Items.GetByIdAsync(transaction.ItemId);

                if (item != null && item.IsAvailable)
                {
                    item.IsAvailable = false;
                    await _unitOfWork.CompleteAsync();
                    _logger.LogInformation("Marked Item {ItemId} as unavailable after completing Transaction {TransactionId}", item.Id, id);
                }
                else if (item == null)
                {
                    _logger.LogWarning("Item {ItemId} not found during CompleteTransactionAsync for Transaction {TransactionId}.", 
                        transaction.ItemId, id);
                }

                await UpdateUserEcoScoresAsync(transaction.SellerId, transaction.BuyerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while completing transaction: {TransactionId}", id);
                throw;
            }
        }

        // Metoda CancelTransactionAsync (bez zmian logiki)
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

        // Metoda CanUserAccessTransactionAsync (bez zmian)
        public async Task<bool> CanUserAccessTransactionAsync(int transactionId, int userId)
        {
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
                if (transaction == null) return false;
                return transaction.BuyerId == userId || transaction.SellerId == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can access transaction. TransactionId: {TransactionId}, UserId: {UserId}", 
                    transactionId, userId);
                return false;
            }
        }

        /// <summary>
        /// Confirms the completion of a transaction by a user in a two-step process.
        /// When both buyer and seller confirm, the transaction is completed.
        /// </summary>
        public async Task ConfirmTransactionCompletionAsync(int id, int userId)
        {
            _logger.LogInformation("Processing transaction confirmation. TransactionId: {TransactionId}, UserId: {UserId}", id, userId);
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
                
                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Transaction with ID {id} not found");
                }

                // Verify that the user is part of the transaction
                if (transaction.BuyerId != userId && transaction.SellerId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to confirm this transaction");
                }

                // Verify that the transaction is in progress
                if (transaction.Status != TransactionStatus.InProgress)
                {
                    throw new InvalidOperationException($"Cannot confirm a transaction with status '{transaction.Status}'");
                }

                // Determine if the user is the buyer or seller
                bool isUserBuyer = (transaction.BuyerId == userId);
                
                // Check if user already confirmed
                if ((isUserBuyer && transaction.BuyerConfirmed) || (!isUserBuyer && transaction.SellerConfirmed))
                {
                    throw new InvalidOperationException("You have already confirmed this transaction");
                }
                
                // Update the confirmation flags based on who is confirming
                if (isUserBuyer)
                {
                    transaction.BuyerConfirmed = true;
                }
                else
                {
                    transaction.SellerConfirmed = true;
                }
                
                // Update last modification date
                transaction.LastUpdateDate = DateTime.UtcNow;

                // If both parties have confirmed, complete the transaction
                if (transaction.BuyerConfirmed && transaction.SellerConfirmed)
                {
                    // Complete the transaction
                    transaction.Status = TransactionStatus.Completed;
                    transaction.EndDate = DateTime.UtcNow;
                    
                    // Mark the item as not available
                    var item = await _unitOfWork.Items.GetByIdAsync(transaction.ItemId);
                    if (item != null && item.IsAvailable)
                    {
                        item.IsAvailable = false;
                        _logger.LogInformation("Marked Item {ItemId} as unavailable after completing Transaction {TransactionId}", 
                            item.Id, id);
                    }
                    
                    // Update eco scores for both parties
                    await UpdateUserEcoScoresAsync(transaction.SellerId, transaction.BuyerId);
                    
                    _logger.LogInformation("Transaction {TransactionId} completed after both parties confirmed", id);
                }
                else
                {
                    _logger.LogInformation("Transaction {TransactionId} partially confirmed by user {UserId}", id, userId);
                }

                // Save changes
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming transaction: {TransactionId}, UserId: {UserId}", id, userId);
                throw;
            }
        }

        /// <summary>
        /// Checks if any transactions should be auto-completed because one party confirmed
        /// more than 14 days ago.
        /// </summary>
        /// <remarks>
        /// This would be called by a background job or schedule (e.g., Hangfire, Quartz.NET)
        /// </remarks>
        public async Task CheckAndAutoCompleteTransactionsAsync()
        {
            try
            {
                // Get all in-progress transactions with one confirmation but not both
                var transactions = await _unitOfWork.Transactions.FindAsync(t => 
                    t.Status == TransactionStatus.InProgress && 
                    ((t.BuyerConfirmed && !t.SellerConfirmed) || (!t.BuyerConfirmed && t.SellerConfirmed)) && // One true, one false
                    t.LastUpdateDate < DateTime.UtcNow.AddDays(-14)); // Last update more than 14 days ago
                    
                foreach (var transaction in transactions)
                {
                    try
                    {
                        // Auto-complete the transaction
                        transaction.Status = TransactionStatus.Completed;
                        transaction.EndDate = DateTime.UtcNow;
                        
                        // Set both confirmations to true
                        transaction.BuyerConfirmed = true;
                        transaction.SellerConfirmed = true;
                        
                        // Mark the item as not available
                        var item = await _unitOfWork.Items.GetByIdAsync(transaction.ItemId);
                        if (item != null && item.IsAvailable)
                        {
                            item.IsAvailable = false;
                        }
                        
                        // Update eco scores
                        await UpdateUserEcoScoresAsync(transaction.SellerId, transaction.BuyerId);
                        
                        _logger.LogInformation("Transaction {TransactionId} auto-completed after 14 days with one-sided confirmation", 
                            transaction.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-completing transaction: {TransactionId}", transaction.Id);
                        // Continue with next transaction
                    }
                }
                
                // Save all changes in a batch
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for transactions to auto-complete");
                throw;
            }
        }
        // Na górze pliku TransactionService.cs dodaj, jeśli brakuje:
// using Microsoft.EntityFrameworkCore;
// using System.Linq;
// W pliku TransactionService.cs

// ... (konstruktor i inne metody bez zmian) ...
// W TransactionService.cs
        public async Task<PagedResult<TransactionDto>> GetAllTransactionsAsync(int pageNumber, int pageSize, TransactionStatus? filterByStatus = null)
        {
            _logger.LogInformation("Fetching all transactions for admin. Page: {PageNumber}, PageSize: {PageSize}, StatusFilter: {StatusFilter}", 
                pageNumber, pageSize, filterByStatus?.ToString() ?? "All");
            try
            {
                // Start with a base query
                var query = _unitOfWork.Transactions.GetAllAsQueryable();
        
                // Apply includes - make sure to use the right method chain
                query = query
                    .Include(t => t.Item)
                    .ThenInclude(i => i.Photos)
                    .Include(t => t.Buyer)
                    .Include(t => t.Seller);

                // Apply status filter if provided
                if (filterByStatus.HasValue)
                {
                    query = query.Where(t => t.Status == filterByStatus.Value);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Get paginated data
                var transactionEntities = await query
                    .OrderByDescending(t => t.StartDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs
                var transactionDtos = _mapper.Map<List<TransactionDto>>(transactionEntities); 

                // Return paged result
                return new PagedResult<TransactionDto>(transactionDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all transactions with pagination for admin. Page: {Page}, Size: {Size}, Status: {Status}",
                    pageNumber, pageSize, filterByStatus);
                throw;
            }
        }

        #region Private Helpers (bez zmian)

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
                    _logger.LogWarning("Unknown current transaction status encountered in IsValidStatusTransition: {CurrentStatus}", 
                        currentStatus);
                    return false;
            }
        }

        private async Task UpdateUserEcoScoresAsync(int sellerId, int buyerId)
        {
            try
            {
                var seller = await _unitOfWork.Users.GetByIdAsync(sellerId);
                var buyer = await _unitOfWork.Users.GetByIdAsync(buyerId);

                if (seller != null && buyer != null)
                {
                    seller.EcoScore += 5;
                    buyer.EcoScore += 3;

                    await _unitOfWork.CompleteAsync();
                    _logger.LogInformation("Updated EcoScores for Seller {SellerId} and Buyer {BuyerId}", sellerId, buyerId);
                }
                else
                {
                    _logger.LogWarning("Could not find seller (ID: {SellerId}) or buyer (ID: {BuyerId}) to update EcoScores.", 
                        sellerId, buyerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating eco scores for seller {SellerId} and buyer {BuyerId}", sellerId, buyerId);
            }
        }

        #endregion
    }
}