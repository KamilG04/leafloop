using LeafLoop.Models;
using LeafLoop.Services.Interfaces; // For ITransactionService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // For UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            UserManager<User> userManager,
            ITransactionService transactionService,
            ILogger<TransactionsController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Serving My Transactions list view page. This view will host a React component.");
            
            // Data is not passed directly; React will fetch it from an API (e.g., /api/transactions).
            // TODO: This MVC action serves a view for a React component. Ensure the corresponding API endpoint
            return View(); // Expects a view at Views/Transactions/Index.cshtml
        }


        // Action for the transaction details page (e.g., /Transactions/Details/5)
        [HttpGet("Transactions/Details/{id:int}")] // Explicit routing for clarity
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Attempting to serve Transaction Details view for TransactionId: {TransactionId}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid Transaction ID requested: {TransactionId}", id);
                // TODO: Localize user-facing error messages. Consider using a resource file.
                return View("Error", new ErrorViewModel { Message = "Invalid Transaction ID." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // This should ideally not happen due to the [Authorize] attribute, but it's a good safeguard.
                _logger.LogWarning("Cannot identify current user for Transaction Details {TransactionId}. User might not be properly authenticated.", id);
                return Challenge(); // Force re-authentication
            }

            try
            {
                // Check if the user has access to this transaction (is buyer or seller).
                // This prevents loading a view for which the subsequent API call by React would fail authorization.
                bool canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);

                if (!canAccess)
                {
                    _logger.LogWarning("User {UserId} forbidden access attempt to Transaction {TransactionId}", user.Id, id);
                    // TODO: Ensure a generic 'AccessDenied.cshtml' view exists in a shared location
                    // (e.g., Views/Shared/) and is appropriately styled for a consistent user experience.
                    return View("AccessDenied");
                }

                _logger.LogInformation("User {UserId} granted access to Transaction Details view for TransactionId: {TransactionId}", user.Id, id);
                // Pass the TransactionId to the view; React will use it to fetch data from the API.
                ViewBag.TransactionId = id;
             
                return View(); // Expects a view at Views/Transactions/Details.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access or serving Transaction Details view for TransactionId: {TransactionId}, UserID: {UserId}", id, user.Id);
                return View("Error", new ErrorViewModel { Message = "An error occurred while loading the transaction details page." });
            }
        }
    }
}