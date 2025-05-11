using System;
using System.Collections.Generic;
using System.Linq; // Potrzebne dla LINQ w logowaniu błędów
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Models.API; // Potrzebne dla ApiResponse
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;
using LeafLoop.Repositories.Interfaces; // Potrzebne dla ApiControllerExtensions (choć część zastępujemy)

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApiAuthPolicy")] // Polityka dla całego kontrolera
    [Produces("application/json")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IMessageService _messageService;
        private readonly IRatingService _ratingService; // Dodaj, jeśli potrzebujesz RateTransaction
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TransactionsController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TransactionsController(
            ITransactionService transactionService,
            IMessageService messageService,
            IRatingService ratingService, // Dodaj wstrzykiwanie
            UserManager<User> userManager,
            ILogger<TransactionsController> logger,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService)); // Przypisz
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // POST: api/transactions
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status201Created)]
        // ... inne ProducesResponseType ...
        public async Task<IActionResult> InitiateTransaction([FromBody] TransactionCreateDto transactionDto)
        {
            // Walidacja DTO
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);
            // Sprawdzenie, czy Type jest poprawną wartością enuma (opcjonalne, ale dobre)
            if (!Enum.IsDefined(typeof(TransactionType), transactionDto.Type))
            {
                 return this.ApiBadRequest($"Invalid transaction type value: {transactionDto.Type}");
            }

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                var transactionId = await _transactionService.InitiateTransactionAsync(transactionDto, userId);
                var createdTransaction = await _transactionService.GetTransactionByIdAsync(transactionId); // Pobierz DTO

                 if (createdTransaction == null) {
                     _logger.LogError("Transaction created (ID: {TransactionId}) but GetTransactionByIdAsync returned null.", transactionId);
                     return this.ApiInternalError("Transaction created but failed to retrieve details.");
                 }

                // Użyj poprawnego rozszerzenia dla CreatedAtAction
                return this.ApiCreatedAtAction(
                    createdTransaction, // Przekaż DTO jako dane
                    nameof(GetTransaction), // Nazwa akcji GET
                    "Transactions",         // Nazwa kontrolera (bez 'Controller')
                    new { id = transactionId } // Parametry routingu
                    // Domyślny komunikat z ApiCreatedAtAction jest OK
                );
            }
            catch (KeyNotFoundException ex) { return this.ApiNotFound(ex.Message); }
            catch (InvalidOperationException ex) { return this.ApiBadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating transaction for ItemId: {ItemId}", transactionDto?.ItemId);
                return this.ApiInternalError("Error initiating transaction", ex);
            }
        }


        // GET: api/transactions
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransactionDto>>), StatusCodes.Status200OK)]
        // ... inne ProducesResponseType ...
        public async Task<IActionResult> GetUserTransactions([FromQuery] bool asSeller = false)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var transactions = await _transactionService.GetTransactionsByUserAsync(user.Id, asSeller);
                // Użyj generycznego ApiOk, kompilator wywnioskuje T = IEnumerable<TransactionDto>
                return this.ApiOk(transactions ?? new List<TransactionDto>()); // Zwróć pustą listę, jeśli null
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user transactions for UserID: {UserId}, asSeller: {AsSeller}", User.FindFirstValue(ClaimTypes.NameIdentifier), asSeller);
                return this.ApiInternalError("Error retrieving transactions", ex);
            }
        }

        // GET: api/transactions/{id:int}
        [HttpGet("{id:int}", Name = "GetTransaction")] // Dodano Name dla CreatedAtAction
        [ProducesResponseType(typeof(ApiResponse<TransactionWithDetailsDto>), StatusCodes.Status200OK)]
        // ... inne ProducesResponseType ...
        public async Task<IActionResult> GetTransaction(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess) return this.ApiForbidden("You are not authorized to view this transaction.");

                var transaction = await _transactionService.GetTransactionWithDetailsAsync(id);
                if (transaction == null) return this.ApiNotFound($"Transaction with ID {id} not found");

                 // Użyj generycznego ApiOk, kompilator wywnioskuje T = TransactionWithDetailsDto
                return this.ApiOk(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction details. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error retrieving transaction details", ex);
            }
        }

         // --- >>> DODANE/POPRAWIONE AKCJE <<< ---

        // PUT: api/transactions/{id:int}/status
        [HttpPut("{id:int}/status")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] // Zmieniono na 200 OK z komunikatem
        // ... inne ProducesResponseType ...
        public async Task<IActionResult> UpdateTransactionStatus(int id, [FromBody] TransactionStatusUpdateDto statusUpdateDto)
        {
             if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
             if (!Enum.IsDefined(typeof(TransactionStatus), statusUpdateDto.Status)) { return this.ApiBadRequest($"Invalid transaction status value: {statusUpdateDto.Status}"); }
             if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                await _transactionService.UpdateTransactionStatusAsync(id, statusUpdateDto.Status, user.Id);

                string successMessage = $"Transaction status updated to {statusUpdateDto.Status}.";
                // Użyj bezpośrednio Ok() z niegenerycznym ApiResponse
                return Ok(ApiResponse.SuccessResponse(successMessage));
            }
            catch (KeyNotFoundException) { return this.ApiNotFound($"Transaction with ID {id} not found"); }
            catch (UnauthorizedAccessException) { return this.ApiForbidden("You are not authorized to update this transaction status."); }
            catch (InvalidOperationException ex) { return this.ApiBadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction status. TransactionId: {TransactionId}, UserID: {UserId}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return this.ApiInternalError("Error updating transaction status", ex);
            }
        }

        // POST: api/transactions/{id:int}/complete
        [HttpPost("{id:int}/complete")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        // ... inne ProducesResponseType ...
        public async Task<IActionResult> CompleteTransaction(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");
                await _transactionService.CompleteTransactionAsync(id, user.Id);
                // Użyj bezpośrednio Ok() z niegenerycznym ApiResponse
                return Ok(ApiResponse.SuccessResponse("Transaction completed successfully"));
            }
            catch (KeyNotFoundException) { return this.ApiNotFound($"Transaction with ID {id} not found"); }
            catch (UnauthorizedAccessException) { return this.ApiForbidden("You are not authorized to complete this transaction."); }
            catch (InvalidOperationException ex) { return this.ApiBadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing transaction. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error completing transaction", ex);
            }
        }

        // POST: api/transactions/{id:int}/cancel
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
         // ... inne ProducesResponseType ...
        public async Task<IActionResult> CancelTransaction(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");
                await _transactionService.CancelTransactionAsync(id, user.Id);
                 // Użyj bezpośrednio Ok() z niegenerycznym ApiResponse
                return Ok(ApiResponse.SuccessResponse("Transaction cancelled successfully"));
            }
            catch (KeyNotFoundException) { return this.ApiNotFound($"Transaction with ID {id} not found"); }
            catch (UnauthorizedAccessException) { return this.ApiForbidden("You are not authorized to cancel this transaction."); }
            catch (InvalidOperationException ex) { return this.ApiBadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling transaction. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error cancelling transaction", ex);
            }
        }

        // GET: api/transactions/{id:int}/messages
        [HttpGet("{id:int}/messages")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MessageDto>>), StatusCodes.Status200OK)]
        // ... inne ProducesResponseType ...
        public async Task<IActionResult> GetTransactionMessages(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess) return this.ApiForbidden("You are not authorized to view messages for this transaction.");

                var messages = await _messageService.GetTransactionMessagesAsync(id);
                // Użyj generycznego ApiOk
                return this.ApiOk(messages ?? new List<MessageDto>());
            }
            catch (KeyNotFoundException) { return this.ApiNotFound($"Transaction with ID {id} not found."); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction messages. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error retrieving transaction messages", ex);
            }
        }

        // POST: api/transactions/{id:int}/messages
        [HttpPost("{id:int}/messages")]
        [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] // Dla przypadku błędu pobrania wiadomości
        // ... inne ProducesResponseType ...
        public async Task<IActionResult> SendTransactionMessage(int id, [FromBody] TransactionMessageDto messageDto)
        {
            _logger.LogInformation("API SendTransactionMessage START for TransactionID: {TransactionId}. Auth User: {AuthUser}", id, User.Identity?.Name ?? "N/A");

            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
            if (messageDto == null || string.IsNullOrWhiteSpace(messageDto.Content)) return this.ApiBadRequest("Message content cannot be empty.");
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess) return this.ApiForbidden("You are not authorized to send messages for this transaction.");

                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null) return this.ApiNotFound($"Transaction with ID {id} not found.");

                if (transaction.Status != TransactionStatus.Pending && transaction.Status != TransactionStatus.InProgress)
                {
                    return this.ApiBadRequest($"Cannot send messages for a transaction with status '{transaction.Status}'.");
                }

                var messageCreateDto = new MessageCreateDto
                {
                    Content = messageDto.Content.Trim(),
                    SenderId = user.Id,
                    ReceiverId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    TransactionId = id
                };

                var messageId = await _messageService.SendMessageAsync(messageCreateDto);
                _logger.LogInformation("Message sent via service. New MessageID: {MessageId} for TransactionID: {TransactionId}", messageId, id);

                var createdMessage = await _messageService.GetMessageByIdAsync(messageId);
                if (createdMessage == null)
                {
                    _logger.LogError("Message sent (ID: {MessageId}) but could not be retrieved for transaction {TransactionId}.", messageId, id);
                    // <<< === POPRAWKA TUTAJ === >>>
                    // Zwróć 200 OK z komunikatem, używając bezpośrednio Ok() i ApiResponse
                    return Ok(ApiResponse.SuccessResponse("Message sent, but could not retrieve details."));
                }

                // Zwróć 200 OK z generycznym ApiOk, bo masz dane (createdMessage)
                return this.ApiOk(createdMessage, "Message sent successfully");
            }
            catch (KeyNotFoundException) { return this.ApiNotFound($"Transaction with ID {id} not found."); }
            catch (UnauthorizedAccessException) { return this.ApiForbidden("You are not authorized for this transaction."); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending transaction message. TransactionId: {TransactionId}, SenderId: {SenderId}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return this.ApiInternalError("Error sending message", ex);
            }
        }

        // POST: api/transactions/{id:int}/ratings
        [HttpPost("{id:int}/ratings")]
        [ProducesResponseType(typeof(ApiResponse<RatingDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RateTransaction(int id, [FromBody] TransactionRatingDto ratingDto)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Sprawdź czy użytkownik ma dostęp do tej transakcji
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess) return this.ApiForbidden("You were not part of this transaction.");

                // Pobierz transakcję, aby znaleźć drugiego użytkownika
                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null) return this.ApiNotFound($"Transaction with ID {id} not found.");

                // Sprawdź czy transakcja jest zakończona
                if (transaction.Status != TransactionStatus.Completed)
                {
                    return this.ApiBadRequest($"Cannot rate a transaction with status '{transaction.Status}'. Only completed transactions can be rated.");
                }

                // Sprawdź czy użytkownik już ocenił tę transakcję
                var existingRating = await _unitOfWork.Ratings.FirstOrDefaultAsync(r => 
                    r.TransactionId == id && r.RaterId == user.Id);
                    
                if (existingRating != null)
                {
                    return this.ApiBadRequest("You have already rated this transaction.");
                }

                // Przygotuj DTO dla serwisu ocen
                var ratingCreateDto = new RatingCreateDto
                {
                    Value = ratingDto.Value,
                    Comment = ratingDto.Comment,
                    RaterId = user.Id,
                    // Oceniamy drugą osobę w transakcji
                    RatedEntityId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    RatedEntityType = RatedEntityType.User, // Zakładamy, że RatedEntityType istnieje
                    TransactionId = id // Powiąż ocenę z transakcją
                };

                // Stwórz ocenę
                var ratingId = await _ratingService.AddRatingAsync(ratingCreateDto);
                
                // Pobierz utworzoną ocenę
                var createdRating = await _ratingService.GetRatingByIdAsync(ratingId);
                if (createdRating == null)
                {
                    _logger.LogError("Rating submitted (ID: {RatingId}) but could not be retrieved for transaction {TransactionId}.", ratingId, id);
                    return this.ApiInternalError("Rating submitted but could not be retrieved.");
                }

                // Zwróć Created (201) z nowym zasobem
                return this.ApiCreatedAtAction(
                    createdRating,
                    nameof(GetTransaction), // Action method for retrieving a transaction
                    "Transactions",         // Controller name
                    new { id },             // Route values
                    "Rating submitted successfully");
            }
            catch (KeyNotFoundException ex) { return this.ApiNotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return this.ApiForbidden(ex.Message); }
            catch (InvalidOperationException ex) { return this.ApiBadRequest(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating transaction. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error rating transaction", ex);
            }
        }
        
        // GET: api/transactions/{id:int}/ratings
        [HttpGet("{id:int}/ratings")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<RatingDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactionRatings(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Verify user has access to this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess) return this.ApiForbidden("You are not authorized to view ratings for this transaction.");

                // Check if transaction exists
                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null) return this.ApiNotFound($"Transaction with ID {id} not found.");

                // Get ratings using repository
                var ratings = await _unitOfWork.Ratings.FindAsync(r => r.TransactionId == id);
                var ratingDtos = _mapper.Map<List<RatingDto>>(ratings);
                
                // Return OK with empty list if none found
                return this.ApiOk(ratingDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction ratings. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error retrieving transaction ratings", ex);
            }
        }
        
        // POST: api/transactions/{id:int}/confirm
        [HttpPost("{id:int}/confirm")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmTransaction(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Get transaction to determine user's role and current state
                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null)
                {
                    return this.ApiNotFound($"Transaction with ID {id} not found");
                }

                // Verify user is authorized (either buyer or seller)
                if (transaction.BuyerId != user.Id && transaction.SellerId != user.Id)
                {
                    return this.ApiForbidden("You are not authorized to confirm this transaction.");
                }

                // Verify transaction is in correct state
                if (transaction.Status != TransactionStatus.InProgress)
                {
                    return this.ApiBadRequest($"Cannot confirm a transaction with status '{transaction.Status}'. Only transactions with status 'InProgress' can be confirmed.");
                }

                // Process the confirmation
                await _transactionService.ConfirmTransactionCompletionAsync(id, user.Id);

                // Return success
                bool isUserBuyer = (transaction.BuyerId == user.Id);
                bool otherPartyConfirmed = isUserBuyer ? transaction.SellerConfirmed : transaction.BuyerConfirmed;
                
                if (otherPartyConfirmed)
                {
                    return this.ApiOk("Transaction has been completed successfully. Both parties have confirmed.");
                }
                else
                {
                    return this.ApiOk("Your confirmation has been recorded. The transaction will be completed when the other party confirms, or automatically after 14 days.");
                }
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return this.ApiForbidden("You are not authorized to confirm this transaction.");
            }
            catch (InvalidOperationException ex)
            {
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming transaction. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error confirming transaction", ex);
            }
        }
    }
}