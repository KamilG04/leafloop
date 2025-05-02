using System;
using System.Collections.Generic;
using System.Security.Claims; // Potrzebne dla User.FindFirstValue
using System.Threading.Tasks;
using LeafLoop.Models;          // Dla User, KeyNotFoundException, UnauthorizedAccessException, TransactionStatus, RatedEntityType (jeśli istnieje)
using LeafLoop.Models.API;      // Dla ApiResponse<T> i ApiResponse
using LeafLoop.Services.DTOs;   // Dla DTOs
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;      // Dla StatusCodes
using Microsoft.AspNetCore.Identity;  // Dla UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;             // <<<=== DODAJ TEN USING dla ApiControllerExtensions

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Wszystkie endpointy transakcji wymagają autoryzacji
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IMessageService _messageService;
        private readonly IRatingService _ratingService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            ITransactionService transactionService,
            IMessageService messageService,
            IRatingService ratingService,
            UserManager<User> userManager,
            ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/transactions
        [HttpGet]
        public async Task<IActionResult> GetUserTransactions( // Zmieniono sygnaturę
            [FromQuery] bool asSeller = false)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var transactions = await _transactionService.GetTransactionsByUserAsync(user.Id, asSeller); // Zakładam, że zwraca IEnumerable<TransactionDto>
                return this.ApiOk(transactions); // Użyj ApiOk<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user transactions for UserID: {UserId}, asSeller: {AsSeller}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, asSeller);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error retrieving transactions", ex);
            }
        }

        // GET: api/transactions/{id:int}
        [HttpGet("{id:int}")] // Dodano :int
        public async Task<IActionResult> GetTransaction(int id) // Zmieniono sygnaturę
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Sprawdź dostęp do transakcji
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    // Użyj ApiForbidden
                    return this.ApiForbidden("You are not authorized to view this transaction.");
                }

                var transaction = await _transactionService.GetTransactionWithDetailsAsync(id); // Zakładam, że zwraca TransactionWithDetailsDto

                if (transaction == null)
                {
                    // Użyj niegenerycznego ApiNotFound
                    return this.ApiNotFound($"Transaction with ID {id} not found");
                }

                // Użyj ApiOk<T>
                return this.ApiOk(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction details. TransactionId: {TransactionId}", id);
                 // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error retrieving transaction details", ex);
            }
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<IActionResult> InitiateTransaction([FromBody] TransactionCreateDto transactionDto) // Zmieniono sygnaturę, dodano FromBody
        {
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Ustaw kupującego jako bieżącego użytkownika
                transactionDto.BuyerId = user.Id;

                var transactionId = await _transactionService.InitiateTransactionAsync(transactionDto);

                // Pobierz DTO utworzonej transakcji do odpowiedzi
                var createdTransaction = await _transactionService.GetTransactionByIdAsync(transactionId); // Zakładam, że zwraca TransactionDto
                if (createdTransaction == null)
                {
                    _logger.LogError("Transaction created (ID: {TransactionId}) but could not be retrieved.", transactionId);
                    // Użyj niegenerycznego ApiInternalError
                    return this.ApiInternalError("Transaction created but could not be retrieved.");
                }

                // Użyj ApiCreatedAtAction
                return this.ApiCreatedAtAction(
                    createdTransaction,
                    nameof(GetTransaction),
                    "Transactions",
                    new { id = transactionId },
                    "Transaction initiated successfully");
            }
            catch (InvalidOperationException ex) // Np. przedmiot niedostępny, próba kupna własnego
            {
                _logger.LogWarning(ex, "Business logic error initiating transaction for ItemId: {ItemId}", transactionDto?.ItemId);
                return this.ApiBadRequest(ex.Message); // Zwróć komunikat błędu biznesowego
            }
            catch (Exception ex) // Inne błędy
            {
                _logger.LogError(ex, "Error initiating transaction for ItemId: {ItemId}", transactionDto?.ItemId);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error initiating transaction", ex);
            }
        }

        // PUT: api/transactions/{id:int}/status
        [HttpPut("{id:int}/status")] // Dodano :int
        public async Task<IActionResult> UpdateTransactionStatus( // Zmieniono sygnaturę
            int id, [FromBody] TransactionStatusUpdateDto statusUpdateDto) // Dodano FromBody
        {
            // Walidacja zgodności ID (TransactionId w DTO może być zbędne)
            // if (id != statusUpdateDto.TransactionId)
            // {
            //     return this.ApiBadRequest("Transaction ID mismatch");
            // }
             if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
             if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Zakładamy, że serwis rzuca KeyNotFound lub UnauthorizedAccess
                await _transactionService.UpdateTransactionStatusAsync(id, statusUpdateDto.Status, user.Id);

                // Użyj niegenerycznego ApiOk z komunikatem
                return this.ApiOk($"Transaction status updated to {statusUpdateDto.Status}");
            }
            catch (KeyNotFoundException)
            {
                 // Użyj niegenerycznego ApiNotFound
                return this.ApiNotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                // Użyj niegenerycznego ApiForbidden
                return this.ApiForbidden("You are not authorized to update this transaction status.");
            }
            catch (InvalidOperationException ex) // Np. nieprawidłowa zmiana statusu
            {
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex) // Inne błędy
            {
                _logger.LogError(ex, "Error updating transaction status. TransactionId: {TransactionId}", id);
                 // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error updating transaction status", ex);
            }
        }

        // POST: api/transactions/{id:int}/complete
        [HttpPost("{id:int}/complete")] // Dodano :int
        public async Task<IActionResult> CompleteTransaction(int id) // Zmieniono sygnaturę
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                // Zakładamy, że serwis rzuca wyjątki
                await _transactionService.CompleteTransactionAsync(id, user.Id);

                // Użyj niegenerycznego ApiOk
                return this.ApiOk("Transaction completed successfully");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return this.ApiForbidden("You are not authorized to complete this transaction.");
            }
            catch (InvalidOperationException ex) // Np. transakcja nie jest w stanie do ukończenia
            {
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing transaction. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error completing transaction", ex);
            }
        }

        // POST: api/transactions/{id:int}/cancel
        [HttpPost("{id:int}/cancel")] // Dodano :int
        public async Task<IActionResult> CancelTransaction(int id) // Zmieniono sygnaturę
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                 // Zakładamy, że serwis rzuca wyjątki
                await _transactionService.CancelTransactionAsync(id, user.Id);

                // Użyj niegenerycznego ApiOk
                return this.ApiOk("Transaction cancelled successfully");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return this.ApiForbidden("You are not authorized to cancel this transaction.");
            }
             catch (InvalidOperationException ex) // Np. transakcja nie jest w stanie do anulowania
            {
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling transaction. TransactionId: {TransactionId}", id);
                return this.ApiInternalError("Error cancelling transaction", ex);
            }
        }

        // GET: api/transactions/{id:int}/messages
        [HttpGet("{id:int}/messages")] // Dodano :int
        public async Task<IActionResult> GetTransactionMessages(int id) // Zmieniono sygnaturę
        {
             if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                     return this.ApiForbidden("You are not authorized to view messages for this transaction.");
                }

                var messages = await _messageService.GetTransactionMessagesAsync(id); // Zakładam, że zwraca IEnumerable<MessageDto>
                return this.ApiOk(messages); // Użyj ApiOk<T>
            }
             catch (KeyNotFoundException) // Jeśli CanUserAccessTransactionAsync lub GetTransactionMessagesAsync rzuci
            {
                return this.ApiNotFound($"Transaction with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction messages. TransactionId: {TransactionId}", id);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error retrieving transaction messages", ex);
            }
        }

        // POST: api/transactions/{id:int}/messages
        [HttpPost("{id:int}/messages")] // Dodano :int
        public async Task<IActionResult> SendTransactionMessage( // Zmieniono sygnaturę
            int id, [FromBody] TransactionMessageDto messageDto) // Dodano FromBody
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return this.ApiUnauthorized("User not found.");

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return this.ApiForbidden("You are not authorized to send messages for this transaction.");
                }

                var transaction = await _transactionService.GetTransactionByIdAsync(id); // Potrzebne do określenia odbiorcy
                 if (transaction == null)
                {
                    return this.ApiNotFound($"Transaction with ID {id} not found.");
                }

                var messageCreateDto = new MessageCreateDto
                {
                    Content = messageDto.Content,
                    SenderId = user.Id,
                    ReceiverId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    TransactionId = id
                };

                var messageId = await _messageService.SendMessageAsync(messageCreateDto);

                // Pobierz utworzone DTO wiadomości do odpowiedzi
                var createdMessage = await _messageService.GetMessageByIdAsync(messageId); // Zakładam, że ta metoda istnieje i zwraca MessageDto
                if (createdMessage == null)
                {
                     _logger.LogError("Message sent (ID: {MessageId}) but could not be retrieved for transaction {TransactionId}.", messageId, id);
                     return this.ApiInternalError("Message sent but could not be retrieved.");
                }

                // Użyj ApiOk<T> zwracając DTO wiadomości
                return this.ApiOk(createdMessage, "Message sent successfully");
            }
            catch (KeyNotFoundException) // Jeśli transakcja nie istnieje
            {
                return this.ApiNotFound($"Transaction with ID {id} not found.");
            }
            catch (UnauthorizedAccessException) // Jeśli CanUserAccessTransactionAsync rzuci
            {
                return this.ApiForbidden("You are not authorized for this transaction.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending transaction message. TransactionId: {TransactionId}", id);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error sending message", ex);
            }
        }

        // POST: api/transactions/{id:int}/ratings
        [HttpPost("{id:int}/ratings")] // Dodano :int
        public async Task<IActionResult> RateTransaction( // Zmieniono sygnaturę
            int id, [FromBody] TransactionRatingDto ratingDto) // Dodano FromBody
        {
             if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");
             if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                 if (user == null) return this.ApiUnauthorized("User not found.");

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                 if (!canAccess)
                {
                    return this.ApiForbidden("You cannot rate this transaction.");
                }

                var transaction = await _transactionService.GetTransactionByIdAsync(id); // Potrzebne do określenia ocenianego
                if (transaction == null)
                {
                     return this.ApiNotFound($"Transaction with ID {id} not found.");
                }

                 // Walidacja, czy użytkownik może ocenić (np. czy transakcja zakończona, czy już ocenił)
                 // Ta logika powinna być w serwisie _ratingService.AddRatingAsync
                 // if (!canRate) return this.ApiBadRequest("Cannot rate this transaction at this time or already rated.");

                var ratingCreateDto = new RatingCreateDto
                {
                    Value = ratingDto.Value,
                    Comment = ratingDto.Comment,
                    RaterId = user.Id,
                    RatedEntityId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    RatedEntityType = RatedEntityType.User, // Upewnij się, że ten enum istnieje
                    TransactionId = id
                };

                var ratingId = await _ratingService.AddRatingAsync(ratingCreateDto); // Zakładam, że AddRatingAsync rzuca wyjątki (np. InvalidOperation dla duplikatu)

                // Pobierz utworzone DTO oceny do odpowiedzi
                 var createdRating = await _ratingService.GetRatingByIdAsync(ratingId); // Zakładam, że ta metoda istnieje i zwraca RatingDto
                 if (createdRating == null)
                 {
                     _logger.LogError("Rating submitted (ID: {RatingId}) but could not be retrieved for transaction {TransactionId}.", ratingId, id);
                     return this.ApiInternalError("Rating submitted but could not be retrieved.");
                 }

                // Użyj ApiOk<T> zwracając DTO oceny
                return this.ApiOk(createdRating, "Rating submitted successfully");
            }
            catch (KeyNotFoundException) // Transakcja nie znaleziona
            {
                return this.ApiNotFound($"Transaction with ID {id} not found.");
            }
            catch (UnauthorizedAccessException) // Nie może ocenić
            {
                return this.ApiForbidden("You are not authorized to rate this transaction.");
            }
            catch (InvalidOperationException ex) // Np. już oceniono, transakcja nie zakończona
            {
                 return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating transaction. TransactionId: {TransactionId}", id);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error rating transaction", ex);
            }
        }

        // GET: api/transactions/{id:int}/ratings
        [HttpGet("{id:int}/ratings")] // Dodano :int
        public async Task<IActionResult> GetTransactionRatings(int id) // Zmieniono sygnaturę
        {
            if (id <= 0) return this.ApiBadRequest("Invalid Transaction ID.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                 if (user == null) return this.ApiUnauthorized("User not found.");

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return this.ApiForbidden("You are not authorized to view ratings for this transaction.");
                }

                var ratings = await _ratingService.GetRatingsByTransactionAsync(id); // Zakładam, że zwraca IEnumerable<RatingDto>
                return this.ApiOk(ratings); // Użyj ApiOk<T>
            }
             catch (KeyNotFoundException) // Jeśli CanUserAccess... lub GetRatings... rzuci
            {
                return this.ApiNotFound($"Transaction with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction ratings. TransactionId: {TransactionId}", id);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error retrieving transaction ratings", ex);
            }
        }
    }

    // Definicje DTO powinny być w plikach DTO
    /*
    using System;
    using LeafLoop.Models; // Dla TransactionStatus
    using System.ComponentModel.DataAnnotations;

    namespace LeafLoop.Services.DTOs
    {
        public class TransactionStatusUpdateDto
        {
            // TransactionId może być niepotrzebne, bo mamy je w URL
            // public int TransactionId { get; set; }

            [Required]
            public TransactionStatus Status { get; set; } // Upewnij się, że TransactionStatus jest zdefiniowane
        }

        public class TransactionMessageDto
        {
            [Required]
            [MaxLength(1000)] // Przykładowy limit długości
            public string Content { get; set; } = null!;
        }

        public class TransactionRatingDto
        {
            [Required]
            [Range(1, 5)] // Zakładając ocenę 1-5
            public int Value { get; set; }

            [MaxLength(500)] // Przykładowy limit
            public string? Comment { get; set; } // Komentarz opcjonalny
        }

        // Przykładowe definicje DTO dla Message i Rating
        public class MessageDto
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public DateTime SentDate { get; set; }
            public int SenderId { get; set; }
            public string SenderName { get; set; } // Można dodać dla wygody
            public int ReceiverId { get; set; }
            public string ReceiverName { get; set; } // Można dodać
            public int? TransactionId { get; set; }
            public bool IsRead { get; set; } // Jeśli śledzisz odczytanie
        }

        public class RatingDto
        {
            public int Id { get; set; }
            public int Value { get; set; }
            public string? Comment { get; set; }
            public DateTime CreatedDate { get; set; }
            public int RaterId { get; set; }
            public string RaterName { get; set; } // Można dodać
            public int RatedEntityId { get; set; } // W tym kontekście ID użytkownika
            public RatedEntityType RatedEntityType { get; set; } // Upewnij się, że enum istnieje
            public int? TransactionId { get; set; }
        }

         public enum RatedEntityType { User, Item, Event } // Przykładowy enum

         // Upewnij się, że TransactionStatus i TransactionType są zdefiniowane w Models
         // public enum TransactionStatus { Initiated, Accepted, Shipped, Completed, Cancelled, Disputed }
         // public enum TransactionType { Exchange, Gift, Sale }
    }
    */
}