using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeafLoop.Api;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryService categoryService,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // GET: api/categories
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return this.ApiOk(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return this.ApiInternalError("Error retrieving categories", ex);
        }
    }

    // GET: api/categories/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Category ID.");

        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);

            if (category == null) return this.ApiNotFound($"Category with ID {id} not found");
            return this.ApiOk(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category. CategoryId: {CategoryId}", id);
            return this.ApiInternalError("Error retrieving category", ex);
        }
    }

    // POST: api/categories
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto categoryDto)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var categoryId = await _categoryService.CreateCategoryAsync(categoryDto);
            var createdCategory = await _categoryService.GetCategoryByIdAsync(categoryId);
            if (createdCategory == null)
            {
                _logger.LogError("Could not retrieve category (ID: {CategoryId}) immediately after creation.",
                    categoryId);
                return this.ApiInternalError("Failed to retrieve category details after creation.");
            }

            return this.ApiCreatedAtAction(
                createdCategory,
                nameof(GetCategory),
                "Categories",
                new { id = categoryId }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category with Name: {CategoryName}", categoryDto?.Name);
            return this.ApiInternalError("Error creating category", ex);
        }
    }

    // PUT: api/categories/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto categoryDto)
    {
        if (id != categoryDto.Id) return this.ApiBadRequest("Category ID mismatch in URL and body.");

        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            await _categoryService.UpdateCategoryAsync(categoryDto);
            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Category with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category. CategoryId: {CategoryId}", id);
            return this.ApiInternalError("Error updating category", ex);
        }
    }

    // DELETE: api/categories/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Category ID.");

        try
        {
            await _categoryService.DeleteCategoryAsync(id);
            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Category with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category. CategoryId: {CategoryId}", id);
            return this.ApiInternalError("Error deleting category", ex);
        }
    }
}