using System;
using System.Collections.Generic; // Potrzebne dla List<T>
using System.Linq; // Potrzebne dla metod LINQ (jeśli używane gdzie indziej)
using System.Security.Claims; // Potrzebne dla User.FindFirstValue
using System.Threading.Tasks;
using AutoMapper; // Potrzebne dla _mapper
using LeafLoop.Models; // Dla User, TransactionStatus
using LeafLoop.Repositories.Interfaces; // Dla IUnitOfWork (w BaseController)
using LeafLoop.Services.DTOs; // Dla DTOs (ItemDto, UserWithDetailsDto, AddressDto, BadgeDto)
using LeafLoop.Services.Interfaces; // Dla serwisów (IUserService, IItemService)
using LeafLoop.ViewModels.Profile; // Dla ProfileViewModel
using LeafLoop.ViewModels; // Dla ErrorViewModel (jeśli tam jest)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Dla UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Potrzebne dla CountAsync w UoW
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    [Authorize] // Cały kontroler wymaga autoryzacji
    public class ProfileController : BaseController // Zakładając, że BaseController udostępnia IUnitOfWork
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly IItemService _itemService; // Serwis przedmiotów
        // private readonly IUnitOfWork _unitOfWork; // Już jest w BaseController
        private readonly IMapper _mapper;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUnitOfWork unitOfWork, // Nadal potrzebne dla BaseController
            UserManager<User> userManager,
            IUserService userService,
            IItemService itemService, // Wstrzyknięty
            IMapper mapper,
            ILogger<ProfileController> logger)
            : base(unitOfWork) // Przekaż UoW do bazy
        {
            _userManager = userManager;
            _userService = userService;
            _itemService = itemService; // Przypisany
            _mapper = mapper;
            _logger = logger;
        }

        // GET: /Profile lub /Profile/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ProfileViewModel viewModel = new ProfileViewModel(); // Inicjalizuj ViewModel na początku

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    _logger.LogError("Profile Index: Could not identify user ID from claims.");
                    return Challenge();
                }

                _logger.LogInformation("Profile Index: Attempting to load profile for UserID: {UserId}", userId);

                // 1. Pobierz szczegóły użytkownika
                var userDetailsDto = await _userService.GetUserWithDetailsAsync(userId);
                if (userDetailsDto == null)
                {
                    _logger.LogWarning("Profile Index: UserWithDetailsDto not found for UserID: {UserId}", userId);
                    return NotFound($"User profile with ID {userId} not found.");
                }
                _mapper.Map(userDetailsDto, viewModel); // Mapuj DTO na ViewModel

                // === POPRAWIONE POBIERANIE PRZEDMIOTÓW ===
                // 2. Pobierz ostatnie przedmioty użytkownika (np. 5) przez serwis
                var recentItemsDto = await _itemService.GetRecentItemsByUserAsync(userId, 5); // Wywołaj metodę serwisu
                viewModel.RecentItems = recentItemsDto?.ToList() ?? new List<ItemDto>(); // Przypisz do ViewModelu
                // === KONIEC POPRAWKI ===

                // 3. Pobierz liczniki (przez UoW jest OK, bo to proste CountAsync)
                // Upewnij się, że TransactionStatus jest poprawnym enumem
                viewModel.TotalItemsCount = await _unitOfWork.Items.CountAsync(i => i.UserId == userId);
                int sellingCount = await _unitOfWork.Transactions.CountAsync(t => t.SellerId == userId && t.Status == TransactionStatus.Completed);
                int buyingCount = await _unitOfWork.Transactions.CountAsync(t => t.BuyerId == userId && t.Status == TransactionStatus.Completed);
                viewModel.TotalTransactionsCount = sellingCount + buyingCount;

                _logger.LogInformation("Profile Index: Successfully loaded data for UserID: {UserId}", userId);

                // 4. Przekaż ViewModel do widoku
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading profile for UserID: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return View("Error", new ErrorViewModel { Message = "An error occurred while loading the profile. Please try again later." });
            }
        }

        // GET: /Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
             var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                _logger.LogError("Profile Edit (GET): Could not identify user ID from claims.");
                return Challenge();
            }
             _logger.LogInformation("Serving Edit Profile view for UserID: {UserId}", userId);
             ViewBag.UserId = userId; // Przekaż ID do widoku dla Reacta
            return View(); // Zwraca Views/Profile/Edit.cshtml
        }

        // Zakomentowana akcja POST Edit bez zmian
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model) { ... }
        */
    }
}
