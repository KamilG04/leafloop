using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;

namespace LeafLoop.Services;

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
        /* ... jak poprzednio ... */
        try
        {
            var transaction =
                await _unitOfWork.Transactions
                    .GetTransactionWithDetailsAsync(id); // Ta metoda repo MUSI ładować wszystkie potrzebne dane
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
        /* ... jak poprzednio ... */
        try
        {
            // Zakładamy, że repozytorium zwraca transakcje z załadowanymi danymi potrzebnymi dla TransactionDto
            var transactions = await _unitOfWork.Transactions.GetTransactionsByUserAsync(userId, asSeller);
            return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting transactions for user: {UserId}, AsSeller: {AsSeller}",
                userId, asSeller);
            throw;
        }
    }

    // Metoda GetTransactionsByItemAsync (bez zmian)
    public async Task<IEnumerable<TransactionDto>> GetTransactionsByItemAsync(int itemId)
    {
        /* ... jak poprzednio ... */
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
        /* ... jak poprzednio ... */
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

    // In TransactionService.cs - update InitiateTransactionAsync method
    public async Task<int> InitiateTransactionAsync(TransactionCreateDto transactionDto, int buyerUserId)
    {
        _logger.LogInformation(
            "Attempting to initiate transaction for ItemId {ItemId} of Type {TransactionType} by UserID {BuyerId}",
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
                    throw new InvalidOperationException(
                        $"Item with ID {transactionDto.ItemId} is not available for transaction.");

                if (item.UserId == buyerUserId)
                    throw new InvalidOperationException("You cannot initiate a transaction for your own item.");

                // 2. Check existing transactions
                var existingTransaction = await _unitOfWork.Transactions.SingleOrDefaultAsync(t =>
                    t.ItemId == transactionDto.ItemId &&
                    t.BuyerId == buyerUserId &&
                    (t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.InProgress)
                );

                if (existingTransaction != null)
                    throw new InvalidOperationException(
                        $"You already have a pending or in-progress transaction (ID: {existingTransaction.Id}) for this item.");

                // 3. Create transaction
                var transaction = new Transaction
                {
                    ItemId = transactionDto.ItemId,
                    BuyerId = buyerUserId,
                    SellerId = item.UserId,
                    StartDate = DateTime.UtcNow,
                    LastUpdateDate = DateTime.UtcNow,
                    Status = TransactionStatus.Pending,
                    Type = transactionDto.Type
                };

                // 4. Update item availability (optimistic concurrency)
                item.IsAvailable = false;

                // 5. Save both changes together in the transaction
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Transaction initiated successfully. TransactionID: {TransactionId}",
                    transaction.Id);
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
        _logger.LogInformation(
            "Attempting to update status for TransactionID: {TransactionId} to {NewStatus} by UserID: {UserId}", id,
            status, userId);
        try
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
            if (transaction == null) throw new KeyNotFoundException($"Transaction with ID {id} not found");

            if (transaction.BuyerId != userId && transaction.SellerId != userId)
                throw new UnauthorizedAccessException("User is not authorized to update this transaction");

            if (!IsValidStatusTransition(transaction.Status, status))
                throw new InvalidOperationException($"Invalid status transition from {transaction.Status} to {status}");

            transaction.Status = status;
            transaction.LastUpdateDate =
                DateTime.UtcNow; // <<< ZAKŁADAM, ŻE DODAŁEŚ TO DO MODELU Transaction I DO BAZY DANYCH

            if (status == TransactionStatus.Completed || status == TransactionStatus.Cancelled)
                transaction.EndDate = DateTime.UtcNow;
            else
                transaction.EndDate = null; // Wyczyść EndDate, jeśli status wraca do nie-końcowego (mało prawdopodobne)

            // _unitOfWork.Transactions.Update(transaction); // EF Core śledzi zmiany, jawne Update nie zawsze konieczne
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Successfully updated status for TransactionID: {TransactionId} to {NewStatus}", id,
                status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while updating transaction status: {TransactionId}, NewStatus: {NewStatus}, UserID: {UserId}",
                id, status, userId);
            throw;
        }
    }

    // Metoda CompleteTransactionAsync (bez zmian logiki)
    public async Task CompleteTransactionAsync(int id, int userId)
    {
        /* ... jak poprzednio ... */
        try
        {
            // Update status to Completed
            await UpdateTransactionStatusAsync(id, TransactionStatus.Completed, userId);

            // Mark item as not available
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning(
                    "Transaction {TransactionId} not found after status update during CompleteTransactionAsync.", id);
                // Można rzucić wyjątek lub po prostu zakończyć
                throw new KeyNotFoundException($"Transaction with ID {id} not found after status update.");
            }

            var item = await _unitOfWork.Items.GetByIdAsync(transaction.ItemId);

            if (item != null && item.IsAvailable)
            {
                item.IsAvailable = false;
                // _unitOfWork.Items.Update(item); // Niekonieczne, jeśli EF śledzi zmiany
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation(
                    "Marked Item {ItemId} as unavailable after completing Transaction {TransactionId}", item.Id, id);
            }
            else if (item == null)
            {
                _logger.LogWarning(
                    "Item {ItemId} not found during CompleteTransactionAsync for Transaction {TransactionId}.",
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
        /* ... jak poprzednio ... */
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
        /* ... jak poprzednio ... */
        try
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
            if (transaction == null) return false;
            return transaction.BuyerId == userId || transaction.SellerId == userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking if user can access transaction. TransactionId: {TransactionId}, UserId: {UserId}",
                transactionId, userId);
            return false;
        }
    }

    #region Private Helpers (bez zmian)

    private bool IsValidStatusTransition(TransactionStatus currentStatus, TransactionStatus newStatus)
    {
        /* ... jak poprzednio ... */
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
                _logger.LogWarning(
                    "Unknown current transaction status encountered in IsValidStatusTransition: {CurrentStatus}",
                    currentStatus);
                return false;
        }
    }

    private async Task UpdateUserEcoScoresAsync(int sellerId, int buyerId)
    {
        /* ... jak poprzednio ... */
        try
        {
            var seller = await _unitOfWork.Users.GetByIdAsync(sellerId);
            var buyer = await _unitOfWork.Users.GetByIdAsync(buyerId);

            if (seller != null && buyer != null)
            {
                seller.EcoScore += 5;
                buyer.EcoScore += 3;

                // _unitOfWork.Users.Update(seller); // Niekonieczne, jeśli EF śledzi zmiany
                // _unitOfWork.Users.Update(buyer);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Updated EcoScores for Seller {SellerId} and Buyer {BuyerId}", sellerId,
                    buyerId);
            }
            else
            {
                _logger.LogWarning(
                    "Could not find seller (ID: {SellerId}) or buyer (ID: {BuyerId}) to update EcoScores.", sellerId,
                    buyerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating eco scores for seller {SellerId} and buyer {BuyerId}", sellerId,
                buyerId);
        }
    }

    #endregion
}