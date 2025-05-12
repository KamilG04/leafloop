using LeafLoop.Models; // Assuming ErrorViewModel is defined here or in a related namespace
using LeafLoop.Services.DTOs; // Assuming CategoryDto is defined here
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Added for ILogger
using System; // Added for ArgumentNullException
using System.Collections.Generic; // Added for List
using System.Threading.Tasks; // Added for Task

namespace LeafLoop.Controllers
{
    // This MVC controller is primarily responsible for serving views that will host React components.
    // Data operations are expected to be handled by separate API controllers called by these React components.
    public class ItemsController : Controller // Inherits from Controller, not ControllerBase, as it serves Views
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

        // GET: /Items or /Items/Index
        // This view is expected to host a React component for displaying/searching items.
        public IActionResult Index()
        {
            _logger.LogInformation("Serving Items Index view, intended for React component mounting.");
            // TODO: This MVC action serves a view for React. Ensure the corresponding API endpoint (e.g., /api/items)
            // for data fetching by the React component is implemented with features like pagination and filtering.
            return View();
        }

        // GET: /Items/Details/5
        // This view is expected to host a React component for displaying item details.
        public IActionResult Details(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid item ID requested for Details view: {ItemId}", id);
                // TODO: Verify that an 'ErrorViewModel' and a corresponding 'Error.cshtml' view exist
                // and are correctly configured for displaying errors.
                // Consider if a more user-friendly "Not Found" page might be better than a generic error for invalid IDs.
                return View("Error",
                    new ErrorViewModel { Message = "Invalid item ID provided." });
            }

            _logger.LogInformation("Serving Item Details view for ItemId: {ItemId}", id);
            ViewBag.ItemId = id; 
            return View();
        }

        // GET: /Items/Create
        // This view displays a form (likely a React component) for adding a new item.
        [Authorize] // Requires user to be logged in.
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Serving Create Item view for an authenticated user.");
            try
            {
                // Fetch categories to populate a dropdown or similar UI element in the form.
                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = categories ?? new List<CategoryDto>();
                // TODO: If the number of categories can be very large, consider if _categoryService.GetAllCategoriesAsync() is optimal.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for the create item form.");
                ViewBag.Categories = new List<CategoryDto>(); // Provide an empty list in case of error.
                // TODO: Localize user-facing error messages.
                ModelState.AddModelError(string.Empty, "Failed to load categories required for the form. Please try again later.");
            }
            return View();
        }

        // GET: /Items/Edit/5
        // This view displays a form (likely a React component) for editing an existing item.
        [Authorize] // Requires user to be logged in.
        [HttpGet] // Explicitly specify HTTP GET.
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
        // This view displays a list of items belonging to the currently logged-in user (likely via a React component).
        [Authorize] // Requires user to be logged in.
        public IActionResult MyItems()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Serving MyItems view for user: {UserId}", userId);
            return View();
        }
    }
}