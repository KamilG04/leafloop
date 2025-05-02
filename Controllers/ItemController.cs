using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Models.API;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
                return BadRequest("Invalid item ID");
            }
            
            ViewBag.ItemId = id;
            return View();
        }

        // GET: /Items/Create
        [Authorize]
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
                ModelState.AddModelError(string.Empty, "Failed to load categories");
            }
            
            return View();
        }

        // GET: /Items/Edit/5
        [Authorize]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid item ID");
            }
            
            ViewBag.ItemId = id;
            return View();
        }

        // GET: /Items/MyItems
        [Authorize]
        public IActionResult MyItems()
        {
            return View();
        }

        // This action serves as a bridge between MVC and the API
        // It uses the same service as the API but returns JSON directly
        [HttpGet]
        public async Task<IActionResult> GetItems(string searchTerm = null, int? categoryId = null, string condition = null, int page = 1, int pageSize = 8)
        {
            try
            {
                var searchDto = new ItemSearchDto
                {
                    SearchTerm = searchTerm,
                    CategoryId = categoryId,
                    Condition = condition,
                    Page = page,
                    PageSize = pageSize
                };
                
                var items = await _itemService.SearchItemsAsync(searchDto);
                var totalItems = await _itemService.GetItemsCountAsync(searchDto);
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
                // Return JSON in the same format as the API
                return Json(new ApiResponse<IEnumerable<ItemDto>>
                {
                    Success = true,
                    Data = items,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Error retrieving items" 
                });
            }
        }
    }
}