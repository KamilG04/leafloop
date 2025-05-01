using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    [Authorize] // Remove this if you want the items page to be accessible to anonymous users
    public class ItemsController : Controller
    {
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ItemsController> _logger;
        private readonly UserManager<User> _userManager;
    
        public ItemsController(
            IItemService itemService,
            ICategoryService categoryService,
            UserManager<User> userManager, // Added parameter
            ILogger<ItemsController> logger)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager)); // Initialize UserManager
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        

        // GET: /Items
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Items/Details/5
        public IActionResult Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Nieprawidłowe ID przedmiotu.");
            }
            
            ViewBag.ItemId = id;
            return View();
        }

        // GET: /Items/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories for create form");
                ViewBag.Categories = new List<CategoryDto>();
                ModelState.AddModelError(string.Empty, "Nie udało się załadować kategorii.");
            }
            
            return View();
        }

        // GET: /Items/Edit/5
        // In ItemsController.cs
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid item ID.");
            }
    
            try
            {
                // Check if item exists
                var item = await _itemService.GetItemByIdAsync(id);
                if (item == null)
                {
                    return NotFound($"Item with ID {id} not found.");
                }
        
                // Get categories for the dropdown
                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
        
                // Set item ID for client-side code
                ViewBag.ItemId = id;
        
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing edit view for item: {ItemId}", id);
                return View("Error");
            }
        }

        // GET: /Items/MyItems
        public IActionResult MyItems()
        {
            return View();
        }

        // GET: /Items/GetItems (AJAX endpoint for React)
        [HttpGet]
        public async Task<IActionResult> GetItems(string searchTerm = null, int? categoryId = null, string condition = null, int page = 1, int pageSize = 8)
        {
            try
            {
                // Create search DTO with pagination
                var searchDto = new ItemSearchDto
                {
                    SearchTerm = searchTerm,
                    CategoryId = categoryId,
                    Condition = condition,
                    Page = page,
                    PageSize = pageSize
                };
                
                // Get items based on search criteria
                var items = await _itemService.SearchItemsAsync(searchDto);
                
                // Get total count for pagination
                var totalItems = await _itemService.GetItemsCountAsync(searchDto);
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
                // Return JSON result
                return Json(new {
                    items = items,
                    totalItems = totalItems,
                    totalPages = totalPages,
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return StatusCode(500, new { error = "Error retrieving items" });
            }
        }

        // Add additional actions as needed...
    }
}