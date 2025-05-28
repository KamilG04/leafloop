using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeafLoop.Controllers
{
    public class ItemsController : Controller
    {
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(
            IItemService itemService,
            ICategoryService categoryService,
            ILogger<ItemsController> logger)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // --- Akcja Index z routingiem atrybutowym ---
        [HttpGet]                // Domyślnie dla metody GET
        [Route("Items")]         // Obsłuży GET /Items
        [Route("Items/Index")]   // Obsłuży GET /Items/Index
        public IActionResult Index()
        {
            _logger.LogInformation(">>> ItemsController.Index() VIA ATTRIBUTE ROUTE - Rendering view for React component.");
            // Teraz normalnie zwracamy widok, który będzie hostował Reacta
            return View();
        }

        // --- Pozostałe akcje ---
        // Możemy zostawić je dla routingu konwencjonalnego,
        // lub dodać jawne atrybuty [Route] dla spójności.

        // GET: /Items/Details/{id}
        [HttpGet("Items/Details/{id:int}")]
        public IActionResult Details(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid item ID requested for Details view: {ItemId}", id);
                return View("Error", new ErrorViewModel { Message = "Invalid item ID provided." });
            }

            _logger.LogInformation("Serving Item Details view for ItemId: {ItemId}", id);
            ViewBag.ItemId = id;
            return View();
        }

        // GET: /Items/Create
        [HttpGet("Items/Create")]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Serving Create Item view for an authenticated user.");
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = categories ?? new List<CategoryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for the create item form.");
                ViewBag.Categories = new List<CategoryDto>();
                ModelState.AddModelError(string.Empty, "Failed to load categories required for the form. Please try again later.");
            }
            return View();
        }

        // GET: /Items/Edit/{id}
        [HttpGet("Items/Edit/{id:int}")]
        [Authorize]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid item ID requested for Edit view: {ItemId}", id);
                return View("Error", new ErrorViewModel { Message = "Invalid item ID provided." });
            }

            _logger.LogInformation("Serving Edit Item view for ItemId: {ItemId}", id);
            ViewBag.ItemId = id;
            return View();
        }

        // GET: /Items/MyItems
        [HttpGet("Items/MyItems")]
        [Authorize]
        public IActionResult MyItems()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Serving MyItems view for user: {UserId}", userId);
            return View();
        }

        // GET: /Items/UserItems?userId={userId} LUB /Items/UserItems/{userId} jeśli zmodyfikujemy route
        [HttpGet("Items/UserItems")] // Można też zrobić [HttpGet("Items/UserItems/{userId:int}")] i pobierać z trasy
        public IActionResult UserItems(int userId) // Parametr z query string ?userId=X
        {
            if (userId <= 0)
            {
                _logger.LogWarning("UserItems: Invalid userId provided: {UserId}", userId);
                return BadRequest("Invalid user ID.");
            }

            _logger.LogInformation("Serving UserItems view for UserID: {UserId}", userId);
            ViewBag.TargetUserId = userId;
            return View(); // Oczekuje widoku Views/Items/UserItems.cshtml
        }
    }
}