using LeafLoop.Models.API; // For ApiResponse and ApiResponse<T>
using LeafLoop.Services.DTOs; // For CategoryDto, CategoryCreateDto, CategoryUpdateDto
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // For StatusCodes
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // For IEnumerable
using System.Threading.Tasks; // For Task

namespace LeafLoop.Api
{
    /// <summary>
    /// Manages categories for items.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
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

        /// <summary>
        /// Retrieves all categories.
        /// </summary>
        /// <returns>A list of all categories.</returns>
        /// <response code="200">Returns the list of categories.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [AllowAnonymous] // Assuming categories are publicly viewable
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllCategories()
        {
            _logger.LogInformation("API GetAllCategories START");
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                _logger.LogInformation("API GetAllCategories SUCCESS. Count: {Count}", categories?.Count() ?? 0);
                return this.ApiOk(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetAllCategories ERROR");
                return this.ApiInternalError("Error retrieving categories.", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to retrieve.</param>
        /// <returns>The category with the specified ID.</returns>
        /// <response code="200">Returns the requested category.</response>
        /// <response code="400">If the category ID is invalid.</response>
        /// <response code="404">If the category with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}", Name = "GetCategoryById")] // Added Name for CreatedAtAction
        [AllowAnonymous] // Assuming categories are publicly viewable
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCategory(int id)
        {
            _logger.LogInformation("API GetCategory START for ID: {CategoryId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("API GetCategory BAD_REQUEST: Invalid Category ID: {CategoryId}", id);
                return this.ApiBadRequest("Invalid Category ID.");
            }

            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);

                if (category == null)
                {
                    _logger.LogWarning("API GetCategory NOT_FOUND: Category with ID {CategoryId} not found", id);
                    return this.ApiNotFound($"Category with ID {id} not found.");
                }
                _logger.LogInformation("API GetCategory SUCCESS for ID: {CategoryId}", id);
                return this.ApiOk(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetCategory ERROR for ID: {CategoryId}", id);
                return this.ApiInternalError("Error retrieving category.", ex);
            }
        }

        /// <summary>
        /// Creates a new category. Requires Admin role.
        /// </summary>
        /// <param name="categoryDto">The data for the new category.</param>
        /// <returns>The newly created category.</returns>
        /// <response code="201">Returns the newly created category and its location.</response>
        /// <response code="400">If the category data is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not an Admin.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto categoryDto)
        {
            _logger.LogInformation("API CreateCategory START for Name: {CategoryName}", categoryDto?.Name ?? "N/A");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API CreateCategory BAD_REQUEST: Invalid model state. Errors: {@ModelStateErrors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var categoryId = await _categoryService.CreateCategoryAsync(categoryDto);
                var createdCategory = await _categoryService.GetCategoryByIdAsync(categoryId);
                if (createdCategory == null)
                {
                    _logger.LogError("API CreateCategory ERROR: Could not retrieve category (ID: {CategoryId}) immediately after creation.", categoryId);
                    return this.ApiInternalError("Failed to retrieve category details after creation.");
                }
                _logger.LogInformation("API CreateCategory SUCCESS. New CategoryID: {CategoryId}, Name: {CategoryName}", categoryId, createdCategory.Name);
                // Corrected call to ApiCreatedAtAction
                return this.ApiCreatedAtAction(
                    createdCategory,                                  // data
                    nameof(GetCategory),                              // actionName
                    this.ControllerContext.ActionDescriptor.ControllerName, // controllerName
                    new { id = categoryId },                          // routeValues
                    "Category created successfully."                  // message
                );
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "API CreateCategory ERROR for Name: {CategoryName}", categoryDto?.Name);
                return this.ApiInternalError("Error creating category.", ex);
            }
        }

        /// <summary>
        /// Updates an existing category. Requires Admin role.
        /// </summary>
        /// <param name="id">The ID of the category to update.</param>
        /// <param name="categoryDto">The updated category data.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Category updated successfully.</response>
        /// <response code="400">If the category data is invalid or ID mismatch.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not an Admin.</response>
        /// <response code="404">If the category with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto categoryDto)
        {
            _logger.LogInformation("API UpdateCategory START for ID: {CategoryId}, New Name: {CategoryName}", id, categoryDto?.Name ?? "N/A");
            if (id != categoryDto.Id)
            {
                _logger.LogWarning("API UpdateCategory BAD_REQUEST: Category ID mismatch in URL ({UrlId}) and body ({BodyId}).", id, categoryDto.Id);
                return this.ApiBadRequest("Category ID mismatch in URL and body.");
            }

            if (!ModelState.IsValid)
            {
                 _logger.LogWarning("API UpdateCategory BAD_REQUEST: Invalid model state for ID {CategoryId}. Errors: {@ModelStateErrors}", id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                await _categoryService.UpdateCategoryAsync(categoryDto);
                _logger.LogInformation("API UpdateCategory SUCCESS for ID: {CategoryId}", id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("API UpdateCategory NOT_FOUND: Category with ID {CategoryId} not found for update.", id);
                return this.ApiNotFound($"Category with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateCategory ERROR for ID: {CategoryId}", id);
                return this.ApiInternalError("Error updating category.", ex);
            }
        }

        /// <summary>
        /// Deletes a category by its ID. Requires Admin role.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Category deleted successfully.</response>
        /// <response code="400">If the category ID is invalid or if the category cannot be deleted (e.g., in use).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not an Admin.</response>
        /// <response code="404">If the category with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            _logger.LogInformation("API DeleteCategory START for ID: {CategoryId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("API DeleteCategory BAD_REQUEST: Invalid Category ID: {CategoryId}", id);
                return this.ApiBadRequest("Invalid Category ID.");
            }

            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                _logger.LogInformation("API DeleteCategory SUCCESS for ID: {CategoryId}", id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("API DeleteCategory NOT_FOUND: Category with ID {CategoryId} not found for deletion.", id);
                return this.ApiNotFound($"Category with ID {id} not found.");
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogWarning(ex, "API DeleteCategory BAD_REQUEST: Cannot delete category ID {CategoryId} due to invalid operation.", id);
                return this.ApiBadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API DeleteCategory ERROR for ID: {CategoryId}", id);
                return this.ApiInternalError("Error deleting category.", ex);
            }
        }
    }
}
