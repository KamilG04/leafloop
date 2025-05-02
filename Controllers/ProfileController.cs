using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Data; // Dodaj using
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces; // Zakładam, że IUnitOfWork tu jest
using LeafLoop.Services.DTOs; // Dodaj using dla DTOs
using LeafLoop.Services.Interfaces; // Dodaj using dla serwisów
using LeafLoop.ViewModels.Profile; // Dodaj using dla ViewModelu
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Dodaj using dla metod EF Core (CountAsync itp.)
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    [Authorize] // Upewnij się, że kontroler wymaga autoryzacji
    public class ProfileController : BaseController // Zakładając, że BaseController udostępnia IUnitOfWork
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService; // Wstrzyknij serwis użytkownika
        private readonly IItemService _itemService; // Wstrzyknij serwis przedmiotów
        private readonly IUnitOfWork _unitOfWork; // Wstrzyknij Unit of Work (jeśli nie ma w BaseController)
        private readonly IMapper _mapper;       // Wstrzyknij AutoMapper
        private readonly ILogger<ProfileController> _logger;
        private readonly LeafLoopDbContext _context;

        public ProfileController(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IUserService userService, // Dodaj do konstruktora
            IItemService itemService, // Dodaj do konstruktora
            IMapper mapper,       // Dodaj do konstruktora
            LeafLoopDbContext context, // <-- WSTRZYKNIJ KONTEKST
            ILogger<ProfileController> logger)
            : base(unitOfWork) // Przekaż UoW do bazy, jeśli trzeba
        {
            _userManager = userManager;
            _userService = userService;
            _itemService = itemService;
            _unitOfWork = unitOfWork; // Przypisz, jeśli nie ma w BaseController
            _mapper = mapper;
            _logger = logger;
            _context = context; // <-- PRZYPISZ KONTEKST
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogWarning(">>> ProfileController.Index - Sprawdzanie uwierzytelnienia: {IsAuth}", User.Identity?.IsAuthenticated ?? false);

            // Pobierz ID zalogowanego użytkownika z ClaimsPrincipal
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                _logger.LogError(">>> ProfileController.Index - Nie można zidentyfikować ID użytkownika.");
                // Zamiast NotFound, lepiej zwrócić Challenge lub AccessDenied, bo użytkownik *powinien* być zalogowany
                return Challenge(); // Wymusi ponowne logowanie lub pokaże błąd dostępu
            }

            _logger.LogWarning(">>> ProfileController.Index - Próba pobrania danych dla UserId: {UserId}", userId);

            // 1. Pobierz podstawowe dane użytkownika + adres (użyj serwisu)
            // Zakładamy, że GetUserWithDetailsAsync ładuje co najmniej adres
            var userDetailsDto = await _userService.GetUserWithDetailsAsync(userId);

            if (userDetailsDto == null)
            {
                _logger.LogError(">>> ProfileController.Index - NIE ZNALEZIONO użytkownika (DTO) dla ID: {UserId}", userId);
                return NotFound($"Nie znaleziono użytkownika o ID {userId}");
            }

             _logger.LogInformation(">>> ProfileController.Index - Pobrano UserWithDetailsDto dla UserId: {UserId}", userId);


            // 2. Pobierz ostatnie przedmioty użytkownika (np. 5) - JAWNIE załaduj potrzebne dane
            // Użyj serwisu lub bezpośrednio UnitOfWork/Repozytorium
            var recentItemsEntities = await _context.Items // <-- Użyj _context.Items (DbSet<Item>)
                .Where(i => i.UserId == userId)             // Filtrowanie LINQ
                .OrderByDescending(i => i.DateAdded)        // Sortowanie LINQ
                .Include(i => i.Category)                   // Dołączanie powiązanych danych (Eager Loading)
                .Take(5)                                    // Limit wyników LINQ
                .ToListAsync();                             // Wykonaj zapytanie

            var recentItemsDto = _mapper.Map<List<ItemDto>>(recentItemsEntities); // Mapowanie zostaje


            // 3. Pobierz liczniki (zamiast ładować całe kolekcje)
            int sellingCount = await _unitOfWork.Transactions.CountAsync(t => t.SellerId == userId && t.Status == TransactionStatus.Completed);
            int buyingCount = await _unitOfWork.Transactions.CountAsync(t => t.BuyerId == userId && t.Status == TransactionStatus.Completed);
            int totalItemsCount = await _unitOfWork.Items.CountAsync(i => i.UserId == userId);
             _logger.LogInformation(">>> ProfileController.Index - Zliczono: Items={ItemCount}, Trans={TransactionCount}", totalItemsCount, sellingCount + buyingCount);

            // 4. Stwórz i wypełnij ViewModel
            var viewModel = new ProfileViewModel
            {
                // Dane z userDetailsDto
                UserId = userDetailsDto.Id,
                FirstName = userDetailsDto.FirstName,
                LastName = userDetailsDto.LastName,
                Email = userDetailsDto.Email,
                AvatarPath = userDetailsDto.AvatarPath,
                EcoScore = userDetailsDto.EcoScore,
                CreatedDate = userDetailsDto.CreatedDate,
                LastActivity = userDetailsDto.LastActivity,
                Address = userDetailsDto.Address, // Zakładamy, że serwis to załadował
                AverageRating = userDetailsDto.AverageRating,
                Badges = userDetailsDto.Badges, // Zakładamy, że serwis to załadował

                // Dane pobrane osobno
                RecentItems = recentItemsDto,
                TotalItemsCount = totalItemsCount,
                TotalTransactionsCount = sellingCount + buyingCount
            };

            _logger.LogWarning(">>> ProfileController.Index - ZNALEZIONO i przygotowano ViewModel dla użytkownika: ID={UserId}", userId);

            // 5. Przekaż ViewModel do widoku
            return View(viewModel);
        }

        // Możesz tu dodać akcję [HttpGet] Edit, która pobierze dane do edycji i zwróci widok z formularzem
        // oraz akcję [HttpPost] Edit do zapisania zmian

    }
}