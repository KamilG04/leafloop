using System;
using System.Collections.Generic; // Potrzebne dla List<CategoryDto>
using System.Threading.Tasks;
using LeafLoop.Models;
// using LeafLoop.Models; // Prawdopodobnie niepotrzebne, jeśli nie używamy modeli bezpośrednio
using LeafLoop.Models.API; // Nadal może być potrzebne, jeśli używasz np. ApiResponse w logach
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
// using LeafLoop.Api; // Raczej niepotrzebne w kontrolerze MVC

namespace LeafLoop.Controllers // Namespace dla kontrolera MVC
{
    public class ItemsController : Controller // Dziedziczy z Controller, nie ControllerBase
    {
        // _itemService może nie być już potrzebny, jeśli ten kontroler tylko serwuje widoki
        // Zostawiamy na razie, na wypadek gdyby był używany w niepokazanych akcjach POST/PUT
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService; // Potrzebny dla akcji Create (GET)
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(
            IItemService itemService, // Zależność nadal tu jest
            ICategoryService categoryService,
            ILogger<ItemsController> logger)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Items lub /Items/Index
        // Ten widok prawdopodobnie hostuje komponent React do wyświetlania/wyszukiwania przedmiotów
        public IActionResult Index()
        {
            _logger.LogInformation("Serving Items Index view.");
            // Ten widok powinien zawierać logikę JS do wywołania /api/items
            return View();
        }

        // GET: /Items/Details/5
        // Ten widok prawdopodobnie hostuje komponent React do wyświetlania szczegółów przedmiotu
        public IActionResult Details(int id)
        {
            if (id <= 0)
            {
                 _logger.LogWarning("Invalid item ID requested for Details view: {ItemId}", id);
                // Można zwrócić BadRequest lub widok błędu
                // return BadRequest("Invalid item ID");
                 return View("Error", new ErrorViewModel { Message = "Invalid item ID provided." }); // Zakładając, że masz ErrorViewModel
            }

             _logger.LogInformation("Serving Item Details view for ItemId: {ItemId}", id);
            ViewBag.ItemId = id; // Przekaż ID do widoku, aby komponent React mógł je pobrać
            return View();
        }

        // GET: /Items/Create
        // Ten widok wyświetla formularz dodawania przedmiotu (prawdopodobnie komponent React)
        [Authorize] // Wymaga zalogowania
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Serving Create Item view.");
            try
            {
                // Pobierz kategorie, aby przekazać je do formularza (np. do dropdowna)
                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = categories ?? new List<CategoryDto>(); // Przekaż kategorie do widoku
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for create form");
                ViewBag.Categories = new List<CategoryDto>(); // Przekaż pustą listę w razie błędu
                // Można dodać błąd do ModelState, aby wyświetlić go w widoku
                ModelState.AddModelError(string.Empty, "Failed to load categories required for the form.");
            }

            return View();
        }

        // GET: /Items/Edit/5
        // Ten widok wyświetla formularz edycji przedmiotu (prawdopodobnie komponent React)
        [Authorize] // Wymaga zalogowania
        [HttpGet] // Jawne określenie metody HTTP
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                 _logger.LogWarning("Invalid item ID requested for Edit view: {ItemId}", id);
                // return BadRequest("Invalid item ID");
                 return View("Error", new ErrorViewModel { Message = "Invalid item ID provided." });
            }

             _logger.LogInformation("Serving Edit Item view for ItemId: {ItemId}", id);
            ViewBag.ItemId = id; // Przekaż ID do widoku dla komponentu React
            return View();
        }

        // GET: /Items/MyItems
        // Ten widok wyświetla listę przedmiotów zalogowanego użytkownika (prawdopodobnie komponent React)
        [Authorize] // Wymaga zalogowania
        public IActionResult MyItems()
        {
             _logger.LogInformation("Serving MyItems view for user: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
             // Widok powinien zawierać logikę JS do wywołania /api/items/my
            return View();
        }

        // === USUNIĘTA METODA GetItems ===
        // Metoda GetItems zwracająca JSON została usunięta.
        // Frontend powinien teraz używać dedykowanego endpointu /api/items.
        // ===============================
    }

    // Przykładowy ErrorViewModel (umieść w odpowiednim miejscu, np. ViewModels/Shared)
    /*
    namespace LeafLoop.ViewModels // Lub inny namespace
    {
        public class ErrorViewModel
        {
            public string RequestId { get; set; }
            public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
            public string Message { get; set; } // Dodatkowa wiadomość o błędzie
        }
    }
    */
}