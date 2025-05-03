using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Models.API;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization; // Upewnij się, że jest
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // <<< USUŃ LUB ZAKOMENTUJ TĘ LINIĘ
    [Authorize(Policy = "ApiAuthPolicy")] // <<< DODAJ TĘ LINIĘ Z POLITYKĄ
    [Produces("application/json")] // Opcjonalnie dla spójności
    public class TransactionsController : ControllerBase
    {
        // ... (reszta kodu kontrolera - konstruktor i akcje - bez zmian) ...

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
            // ... implementacja konstruktora ...
             _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/transactions
        [HttpGet]
        // Nie potrzebujesz już [Authorize] tutaj, bo jest na kontrolerze
        public async Task<IActionResult> GetUserTransactions([FromQuery] bool asSeller = false)
        {
           // ... implementacja akcji ...
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
         // Nie potrzebujesz już [Authorize] tutaj, bo jest na kontrolerze
        public async Task<IActionResult> GetTransaction(int id) // Zmieniono sygnaturę
        {
            // ... implementacja akcji ...
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


        // ... (WSZYSTKIE INNE AKCJE POZOSTAJĄ BEZ ZMIAN) ...
        // Nie potrzebują indywidualnych atrybutów [Authorize], jeśli jest on na poziomie kontrolera.

    }
}