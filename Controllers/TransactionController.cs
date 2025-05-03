// Ścieżka: Controllers/TransactionsController.cs
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LeafLoop.Models; // Dla User
using LeafLoop.Services.Interfaces; // Dla ITransactionService
using LeafLoop.ViewModels; // Dla ErrorViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Dla UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    [Authorize] // Wymaga zalogowania do wszystkich akcji w tym kontrolerze
    public class TransactionsController : Controller // Dziedziczy z Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionsController> _logger;

        // Wstrzyknij potrzebne serwisy
        public TransactionsController(
            UserManager<User> userManager,
            ITransactionService transactionService,
            ILogger<TransactionsController> logger)
        {
            _userManager = userManager;
            _transactionService = transactionService;
            _logger = logger;
        }

        // Akcja dla strony listy transakcji (np. /Transactions)
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Serving My Transactions list view page.");
            // Ten widok będzie hostem dla komponentu React MyTransactionsList.js
            // Nie przekazujemy tu danych, React pobierze je z API /api/transactions
            return View(); // Oczekuje widoku Views/Transactions/Index.cshtml
        }


        // Akcja dla strony szczegółów transakcji (np. /Transactions/Details/5)
        [HttpGet("Transactions/Details/{id:int}")] // Jawny routing dla pewności
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Attempting to serve Transaction Details view for TransactionId: {TransactionId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid Transaction ID requested: {TransactionId}", id);
                return View("Error", new ErrorViewModel { Message = "Nieprawidłowe ID transakcji." });
            }

            // Pobierz bieżącego użytkownika
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Cannot identify current user for Transaction Details {TransactionId}", id);
                // Użytkownik powinien być zalogowany przez [Authorize], ale na wszelki wypadek
                return Challenge(); // Wymuś ponowne logowanie
            }

            try
            {
                // Sprawdź, czy użytkownik ma dostęp do tej transakcji (jest kupującym lub sprzedającym)
                // Zapobiega to ładowaniu strony, do której użytkownik i tak nie zobaczy danych z API
                bool canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);

                if (!canAccess)
                {
                    _logger.LogWarning("User {UserId} forbidden access attempt to Transaction {TransactionId}", user.Id, id);
                    // Zwróć widok "Access Denied" lub inny błąd
                    // return Forbid(); // To zwróciłoby 403, ale przeglądarka może nie obsłużyć dobrze
                    return View("AccessDenied"); // Załóżmy, że masz widok Views/Shared/AccessDenied.cshtml
                }

                 _logger.LogInformation("User {UserId} granted access to Transaction Details view for TransactionId: {TransactionId}", user.Id, id);
                // Przekaż ID transakcji do widoku, React użyje go do pobrania danych z API
                ViewBag.TransactionId = id;
                return View(); // Oczekuje widoku Views/Transactions/Details.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access or serving Transaction Details view for TransactionId: {TransactionId}, UserID: {UserId}", id, user.Id);
                return View("Error", new ErrorViewModel { Message = "Wystąpił błąd podczas ładowania strony szczegółów transakcji." });
            }
        }
    }
}