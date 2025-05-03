using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;          // Potrzebne, jeśli np. KeyNotFoundException jest specyficzne dla modelu
using LeafLoop.Models.API;      // Dla ApiResponse<T> i ApiResponse
using LeafLoop.Services.DTOs;   // Dla DTOs
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;      // Dla StatusCodes
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;             // Dla ApiControllerExtensions

namespace LeafLoop.Api
{
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
        // === ZMIANA SYGNATURY ===
        // Zmieniono Task<ActionResult<IEnumerable<CategoryDto>>> na Task<IActionResult>
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                // Użyj ApiOk<T> zwracającego dane
                return this.ApiOk(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                // Użyj ApiInternalError
                return this.ApiInternalError("Error retrieving categories", ex);
            }
        }

        // GET: api/categories/{id}
        [HttpGet("{id:int}")] // Dodano :int dla routingu
        // === ZMIANA SYGNATURY ===
        // Zmieniono Task<ActionResult<CategoryDto>> na Task<IActionResult>
        public async Task<IActionResult> GetCategory(int id)
        {
            // Podstawowa walidacja
            if (id <= 0)
            {
                 return this.ApiBadRequest("Invalid Category ID.");
            }

            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);

                if (category == null)
                {
                    // Użyj ApiNotFound
                    return this.ApiNotFound($"Category with ID {id} not found");
                }

                // Użyj ApiOk<T> zwracającego dane
                return this.ApiOk(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category. CategoryId: {CategoryId}", id);
                // Użyj ApiInternalError
                return this.ApiInternalError("Error retrieving category", ex);
            }
        }

        // POST: api/categories
        [HttpPost]
        [Authorize(Roles = "Admin")] // Zakładając, że tylko admin może tworzyć kategorie
        // === ZMIANA SYGNATURY ===
        // Zmieniono Task<ActionResult<int>> na Task<IActionResult> (zwrócimy całe DTO, nie tylko ID)
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto categoryDto) // Zmieniono typ parametru na FromBody
        {
             // Walidacja modelu (jeśli używasz atrybutów w DTO)
             if (!ModelState.IsValid)
             {
                 return this.ApiBadRequest(ModelState);
             }

            try
            {
                // Utwórz kategorię i pobierz ID
                var categoryId = await _categoryService.CreateCategoryAsync(categoryDto);

                // Pobierz utworzoną kategorię jako DTO, aby zwrócić ją w odpowiedzi
                var createdCategory = await _categoryService.GetCategoryByIdAsync(categoryId);
                if(createdCategory == null)
                {
                    // Coś poszło nie tak, jeśli nie możemy odzyskać właśnie utworzonej kategorii
                    _logger.LogError("Could not retrieve category (ID: {CategoryId}) immediately after creation.", categoryId);
                    return this.ApiInternalError("Failed to retrieve category details after creation.");
                }

                // Użyj ApiCreatedAtAction zwracającego 201 Created z lokalizacją i danymi
                return this.ApiCreatedAtAction(
                    createdCategory,          // Zwracany obiekt
                    nameof(GetCategory),      // Nazwa akcji do pobrania zasobu
                    "Categories",             // Nazwa kontrolera
                    new { id = categoryId }   // Parametry routingu dla lokalizacji
                    // Opcjonalny komunikat można dodać na końcu
                );
            }
            catch (Exception ex) // Złap bardziej specyficzne wyjątki, jeśli możliwe (np. DbUpdateException)
            {
                _logger.LogError(ex, "Error creating category with Name: {CategoryName}", categoryDto?.Name);
                // Użyj ApiInternalError
                return this.ApiInternalError("Error creating category", ex);
            }
        }

        // PUT: api/categories/{id}
        [HttpPut("{id:int}")] // Dodano :int
        [Authorize(Roles = "Admin")]
        // Sygnatura już jest poprawna: Task<IActionResult>
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto categoryDto) // Zmieniono typ parametru na FromBody
        {
            // Sprawdź zgodność ID
            if (id != categoryDto.Id)
            {
                // Użyj ApiBadRequest
                return this.ApiBadRequest("Category ID mismatch in URL and body.");
            }

             // Walidacja modelu
            if (!ModelState.IsValid)
            {
                 return this.ApiBadRequest(ModelState);
            }

            try
            {
                // UpdateCategoryAsync powinien rzucić KeyNotFoundException, jeśli kategoria nie istnieje
                await _categoryService.UpdateCategoryAsync(categoryDto);
                // Użyj ApiNoContent dla sukcesu bez zwracania danych
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException) // Łapanie specyficznego wyjątku z serwisu
            {
                 // Użyj ApiNotFound
                return this.ApiNotFound($"Category with ID {id} not found");
            }
            catch (Exception ex) // Inne, niespodziewane błędy
            {
                _logger.LogError(ex, "Error updating category. CategoryId: {CategoryId}", id);
                 // Użyj ApiInternalError
                return this.ApiInternalError("Error updating category", ex);
            }
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id:int}")] // Dodano :int
        [Authorize(Roles = "Admin")]
         // Sygnatura już jest poprawna: Task<IActionResult>
        public async Task<IActionResult> DeleteCategory(int id)
        {
             if (id <= 0)
            {
                 return this.ApiBadRequest("Invalid Category ID.");
            }

            try
            {
                 // DeleteCategoryAsync powinien rzucić KeyNotFoundException lub InvalidOperationException
                await _categoryService.DeleteCategoryAsync(id);
                // Użyj ApiNoContent dla sukcesu
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException) // Kategoria nie znaleziona
            {
                // Użyj ApiNotFound
                return this.ApiNotFound($"Category with ID {id} not found");
            }
            catch (InvalidOperationException ex) // Błąd logiki biznesowej (np. nie można usunąć, bo ma powiązane itemy)
            {
                // Użyj ApiBadRequest, przekazując komunikat z wyjątku
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex) // Inne, niespodziewane błędy
            {
                _logger.LogError(ex, "Error deleting category. CategoryId: {CategoryId}", id);
                // Użyj ApiInternalError
                return this.ApiInternalError("Error deleting category", ex);
            }
        }
    }
}