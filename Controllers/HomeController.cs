using System.Diagnostics;
using System.Security.Claims;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.Interfaces;
using LeafLoop.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
namespace LeafLoop.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IItemService _itemService;
        private readonly IUserService _userService;
        private readonly IEventService _eventService;
        private readonly ITransactionService _transactionService;

        public HomeController(
            IUnitOfWork unitOfWork,
            ILogger<HomeController> logger,
            IItemService itemService,
            IUserService userService,
            IEventService eventService,
            ITransactionService transactionService)
            : base(unitOfWork)
        {
            _logger = logger;
            _itemService = itemService;
            _userService = userService;
            _eventService = eventService;
            _transactionService = transactionService;
        }

        public async Task<IActionResult> Index()
        {
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string userName = User.Identity?.Name; // Często email

            _logger.LogWarning(">>> HomeController.Index - Stan Uwierzytelnienia: {IsAuth}", isAuthenticated);
            _logger.LogWarning(">>> HomeController.Index - User ID (Claim): {UserId}", userId ?? "BRAK");
            _logger.LogWarning(">>> HomeController.Index - User Name (Identity): {UserName}", userName ?? "BRAK");
            // --- KONIEC LOGOWANIA ---
            try
            {
                var viewModel = new HomeViewModel();
                
                // Get recent items (last 12 items)
                viewModel.RecentItems = (await _itemService.GetRecentItemsAsync(12)).ToList();
                
                // Get featured items (can use a different service method if you have one, or just use the first 5 recent items)
                viewModel.FeaturedItems = viewModel.RecentItems.Take(5).ToList();
                
                // Get top users by EcoScore
                viewModel.TopUsers = (await _userService.GetTopUsersByEcoScoreAsync(6)).ToList();
                
                // Get upcoming events
                viewModel.UpcomingEvents = (await _eventService.GetUpcomingEventsAsync(4)).ToList();
                
                // Get platform statistics (if you want to show them)
                viewModel.Stats = new StatsSummaryDto
                {
                    TotalItems = await _unitOfWork.Items.CountAsync(),
                    TotalUsers = await _unitOfWork.Users.CountAsync(),
                    CompletedTransactions = await _unitOfWork.Transactions.CountAsync(t => t.Status == TransactionStatus.Completed),
                    TotalEvents = await _unitOfWork.Events.CountAsync()
                };
                
                // Get categories for navigation (if needed)
                ViewBag.Categories = await _unitOfWork.Categories.GetAllAsync();
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while building home page");
                return View("Error");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
    
}