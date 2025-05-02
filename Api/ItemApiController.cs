using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims; // Potrzebne dla User.FindFirstValue
using System.IO; // Potrzebne dla Path.GetFileName
using LeafLoop.Models;
using LeafLoop.Models.API;      // Dla ApiResponse<T> i ApiResponse
// using LeafLoop.Middleware; // Prawdopodobnie niepotrzebne w kontrolerze API
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;             // <<<=== DODAJ TEN USING dla ApiControllerExtensions

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly IPhotoService _photoService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(
            IItemService itemService,
            IPhotoService photoService,
            UserManager<User> userManager,
            ILogger<ItemsController> logger)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/items
        [HttpGet]
        public async Task<IActionResult> GetItems([FromQuery] ItemSearchDto searchDto) // Zmieniono sygnaturę na IActionResult
        {
            try
            {
                searchDto.Page ??= 1;
                searchDto.PageSize ??= 8;

                // Walidacja Page/PageSize
                if (searchDto.Page <= 0) searchDto.Page = 1;
                if (searchDto.PageSize <= 0 || searchDto.PageSize > 100) searchDto.PageSize = 8; // Ogranicz rozmiar strony

                var items = await _itemService.SearchItemsAsync(searchDto);
                var totalItems = await _itemService.GetItemsCountAsync(searchDto);
                var totalPages = (int)Math.Ceiling((double)totalItems / searchDto.PageSize.Value);

                // Użycie ApiOkWithPagination - jest już poprawne
                return this.ApiOkWithPagination(items, totalItems, totalPages, searchDto.Page.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items with search: {@SearchDto}", searchDto);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error retrieving items", ex);
            }
        }

        // GET: api/items/{id:int}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetItem(int id) // Zmieniono sygnaturę na IActionResult
        {
            if (id <= 0)
            {
                // Użyj niegenerycznego ApiBadRequest
                return this.ApiBadRequest("Invalid item ID.");
            }

            try
            {
                var item = await _itemService.GetItemWithDetailsAsync(id); // Zakładam, że zwraca ItemWithDetailsDto

                if (item == null)
                {
                     // Użyj niegenerycznego ApiNotFound
                    return this.ApiNotFound($"Item with ID {id} not found.");
                }

                // Użycie ApiOk<T> - jest poprawne
                return this.ApiOk(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item details. ItemId: {ItemId}", id);
                 // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error retrieving item details", ex);
            }
        }

        // GET: api/items/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserItems() // Zmieniono sygnaturę na IActionResult
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    // Użyj niegenerycznego ApiUnauthorized
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                var items = await _itemService.GetItemsByUserAsync(userId); // Zakładam, że zwraca IEnumerable<ItemDto>
                // Użycie ApiOk<T> - jest poprawne
                return this.ApiOk(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user items");
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error retrieving your items", ex);
            }
        }

        // POST: api/items
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateItem([FromBody] ItemCreateDto itemDto) // Zmieniono sygnaturę na IActionResult
        {
            if (!ModelState.IsValid)
            {
                 // Użyj niegenerycznego ApiBadRequest z ModelState
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    // Użyj niegenerycznego ApiUnauthorized
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                var itemId = await _itemService.AddItemAsync(itemDto, user.Id);
                var createdItemDto = await _itemService.GetItemByIdAsync(itemId); // Zakładam, że zwraca ItemDto

                if (createdItemDto == null)
                {
                    _logger.LogError("Could not retrieve the item (ID: {ItemId}) right after creation", itemId);
                    // Użyj niegenerycznego ApiInternalError
                    return this.ApiInternalError("Error retrieving created item.");
                }

                // Użyj ApiCreatedAtAction zwracającego DTO
                return this.ApiCreatedAtAction(
                    createdItemDto,
                    nameof(GetItem),
                    "Items",
                    new { id = itemId },
                    "Item created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item. DTO: {@ItemDto}", itemDto);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error creating item", ex);
            }
        }

        // PUT: api/items/{id:int}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateDto itemDto) // Zmieniono sygnaturę na IActionResult
        {
            if (id != itemDto.Id)
            {
                // Użyj niegenerycznego ApiBadRequest
                return this.ApiBadRequest("Item ID mismatch in URL and body.");
            }

            if (!ModelState.IsValid)
            {
                 // Użyj niegenerycznego ApiBadRequest z ModelState
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                     // Użyj niegenerycznego ApiUnauthorized
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                // Zakładamy, że UpdateItemAsync rzuca KeyNotFound lub UnauthorizedAccess
                await _itemService.UpdateItemAsync(itemDto, user.Id);

                // Użyj niegenerycznego ApiOk z komunikatem
                return this.ApiOk("Item updated successfully");
            }
            catch (KeyNotFoundException ex) // Z serwisu
            {
                 // Użyj niegenerycznego ApiNotFound
                return this.ApiNotFound(ex.Message); // Przekaż komunikat z wyjątku
            }
            catch (UnauthorizedAccessException) // Z serwisu
            {
                 // Użyj niegenerycznego ApiForbidden
                return this.ApiForbidden("You are not authorized to update this item.");
            }
            catch (Exception ex) // Inne błędy
            {
                _logger.LogError(ex, "Error updating item. ItemId: {ItemId}, DTO: {@ItemDto}", id, itemDto);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error updating item", ex);
            }
        }

        // DELETE: api/items/{id:int}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteItem(int id) // Zmieniono sygnaturę na IActionResult
        {
            if (id <= 0)
            {
                 // Użyj niegenerycznego ApiBadRequest
                return this.ApiBadRequest("Invalid item ID.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    // Użyj niegenerycznego ApiUnauthorized
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                // Zakładamy, że DeleteItemAsync rzuca KeyNotFound lub UnauthorizedAccess
                await _itemService.DeleteItemAsync(id, user.Id);

                // Użyj niegenerycznego ApiOk z komunikatem
                return this.ApiOk("Item deleted successfully");
            }
            catch (KeyNotFoundException ex) // Z serwisu
            {
                // Użyj niegenerycznego ApiNotFound
                return this.ApiNotFound(ex.Message);
            }
            catch (UnauthorizedAccessException) // Z serwisu
            {
                 // Użyj niegenerycznego ApiForbidden
                return this.ApiForbidden("You are not authorized to delete this item.");
            }
            catch (Exception ex) // Inne błędy
            {
                _logger.LogError(ex, "Error deleting item. ItemId: {ItemId}", id);
                 // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error deleting item", ex);
            }
        }

        // POST: api/items/{id:int}/photos
        [HttpPost("{id:int}/photos")]
        [Authorize]
        // Zmieniono sygnaturę na IActionResult, zwrócimy ApiOk<PhotoDto>
        public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
        {
            // Walidacja wejściowa
            if (id <= 0)
                return this.ApiBadRequest("Invalid item ID.");
            if (photo == null || photo.Length == 0)
                return this.ApiBadRequest("No photo file was uploaded.");
            if (photo.Length > 5 * 1024 * 1024) // Limit 5MB
                return this.ApiBadRequest("File size exceeds maximum of 5MB.");
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
                return this.ApiBadRequest("Invalid file type (allowed: JPG, PNG, WEBP).");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return this.ApiUnauthorized("Unable to identify user.");

                // Sprawdź, czy użytkownik jest właścicielem przedmiotu
                if (!await _itemService.IsItemOwnerAsync(id, user.Id)) // Zakładamy, że ta metoda istnieje
                {
                    return this.ApiForbidden("You are not authorized to upload photos for this item.");
                }

                // Upload pliku i zapis w bazie danych
                string photoPath;
                using (var stream = photo.OpenReadStream())
                {
                    // Zakładam, że UploadPhotoAsync zwraca ścieżkę względną lub URL
                    photoPath = await _photoService.UploadPhotoAsync(stream, photo.FileName, photo.ContentType);
                }

                var photoCreateDto = new PhotoCreateDto
                {
                    Path = photoPath,
                    FileName = Path.GetFileName(photo.FileName), // Bezpieczne pobranie nazwy pliku
                    FileSize = photo.Length,
                    ItemId = id
                };

                var photoId = await _photoService.AddPhotoAsync(photoCreateDto, user.Id);
                var createdPhotoDto = await _photoService.GetPhotoByIdAsync(photoId); // Zakładam, że zwraca PhotoDto

                if (createdPhotoDto == null)
                {
                    _logger.LogError("Could not retrieve photo info (ID: {PhotoId}) after upload for item {ItemId}", photoId, id);
                    return this.ApiInternalError("Error retrieving uploaded photo information.");
                }

                // Zwróć 200 OK z danymi utworzonego zdjęcia zamiast 201 Created
                // Jest to prostsze i często wystarczające dla operacji uploadu.
                // Jeśli potrzebujesz 201 z nagłówkiem Location, trzeba by stworzyć endpoint GetPhoto.
                return this.ApiOk(createdPhotoDto, "Photo uploaded successfully");

            }
            catch (UnauthorizedAccessException) // Dodatkowe zabezpieczenie, jeśli IsItemOwnerAsync rzuci
            {
                return this.ApiForbidden("You are not authorized to upload photos for this item.");
            }
            catch (KeyNotFoundException ex) // Jeśli item nie istnieje
            {
                return this.ApiNotFound(ex.Message); // Np. "Item with ID {id} not found"
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for item. ItemId: {ItemId}", id);
                return this.ApiInternalError("Error uploading photo", ex);
            }
        }

        // DELETE: api/items/{itemId:int}/photos/{photoId:int}
        [HttpDelete("{itemId:int}/photos/{photoId:int}")]
        [Authorize]
        public async Task<IActionResult> DeletePhoto(int itemId, int photoId) // Zmieniono sygnaturę na IActionResult
        {
            if (itemId <= 0 || photoId <= 0)
            {
                // Użyj niegenerycznego ApiBadRequest
                return this.ApiBadRequest("Invalid item or photo ID.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                     // Użyj niegenerycznego ApiUnauthorized
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                // Sprawdzenie uprawnień (właściciel przedmiotu) powinno być wewnątrz serwisu
                // lub tutaj, jeśli serwis tego nie robi
                if (!await _itemService.IsItemOwnerAsync(itemId, user.Id)) // Ponowne użycie IsItemOwnerAsync
                {
                     return this.ApiForbidden("You are not authorized to delete photos for this item.");
                }

                // Zakładamy, że DeletePhotoAsync rzuca KeyNotFound lub UnauthorizedAccess
                await _photoService.DeletePhotoAsync(photoId, user.Id);

                // Użyj niegenerycznego ApiOk z komunikatem
                return this.ApiOk("Photo deleted successfully");
            }
            catch (KeyNotFoundException ex) // Z serwisu (np. zdjęcie nie istnieje)
            {
                // Użyj niegenerycznego ApiNotFound
                return this.ApiNotFound(ex.Message);
            }
            catch (UnauthorizedAccessException) // Z serwisu (np. próba usunięcia nie swojego zdjęcia)
            {
                 // Użyj niegenerycznego ApiForbidden
                // Można dodać bardziej szczegółowy komunikat
                return this.ApiForbidden("You are not authorized to delete this photo.");
            }
            catch (Exception ex) // Inne błędy
            {
                _logger.LogError(ex, "Error deleting photo. PhotoId: {PhotoId}, ItemId: {ItemId}", photoId, itemId);
                // Użyj niegenerycznego ApiInternalError
                return this.ApiInternalError("Error deleting photo", ex);
            }
        }
    }
}