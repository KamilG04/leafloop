using System.Security.Claims;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LeafLoop.Middleware;


namespace LeafLoop.Api;

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

    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] ItemSearchDto searchDto)
    {
        try
        {
            searchDto.Page ??= 1;
            searchDto.PageSize ??= 8;

            if (searchDto.Page <= 0) searchDto.Page = 1;
            if (searchDto.PageSize <= 0 || searchDto.PageSize > 100) searchDto.PageSize = 8;

            var items = await _itemService.SearchItemsAsync(searchDto);
            var totalItems = await _itemService.GetItemsCountAsync(searchDto);
            var totalPages = (int)Math.Ceiling((double)totalItems / searchDto.PageSize.Value);

            return this.ApiOkWithPagination(items, totalItems, totalPages, searchDto.Page.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items with search: {@SearchDto}", searchDto);
            return this.ApiInternalError("Error retrieving items", ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetItem(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid item ID.");

        try
        {
            var item = await _itemService.GetItemWithDetailsAsync(id);

            if (item == null) return this.ApiNotFound($"Item with ID {id} not found.");

            return this.ApiOk(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item details. ItemId: {ItemId}", id);
            return this.ApiInternalError("Error retrieving item details", ex);
        }
    }

    [HttpGet("my")]
    [Authorize(Policy = "ApiAuthPolicy")]
    public async Task<IActionResult> GetCurrentUserItems()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                return this.ApiUnauthorized("Unable to identify user.");

            var items = await _itemService.GetItemsByUserAsync(userId);
            return this.ApiOk(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user items");
            return this.ApiInternalError("Error retrieving your items", ex);
        }
    }

    [HttpPost]
    [Authorize(Policy = "ApiAuthPolicy")]
    public async Task<IActionResult> CreateItem([FromBody] ItemCreateDto itemDto)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return this.ApiUnauthorized("Unable to identify user.");

            var itemId = await _itemService.AddItemAsync(itemDto, user.Id);
            var createdItemDto = await _itemService.GetItemByIdAsync(itemId);

            if (createdItemDto == null)
            {
                _logger.LogError("Could not retrieve the item (ID: {ItemId}) right after creation", itemId);
                return this.ApiInternalError("Error retrieving created item.");
            }

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
            return this.ApiInternalError("Error creating item", ex);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ApiAuthPolicy")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateDto itemDto)
    {
        if (id != itemDto.Id) return this.ApiBadRequest("Item ID mismatch in URL and body.");

        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return this.ApiUnauthorized("Unable to identify user.");

            await _itemService.UpdateItemAsync(itemDto, user.Id);
            return this.ApiOk("Item updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.ApiNotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbidden("You are not authorized to update this item.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item. ItemId: {ItemId}, DTO: {@ItemDto}", id, itemDto);
            return this.ApiInternalError("Error updating item", ex);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ApiAuthPolicy")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid item ID.");

        try
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return this.ApiUnauthorized("Unable to identify user.");

            await _itemService.DeleteItemAsync(id, user.Id);
            return this.ApiOk("Item deleted successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.ApiNotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbidden("You are not authorized to delete this item.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item. ItemId: {ItemId}", id);
            return this.ApiInternalError("Error deleting item", ex);
        }
    }

    [HttpPost("{id:int}/photos")]
    [Authorize(Policy = "ApiAuthPolicy")]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
    {
        if (id <= 0)
            return this.ApiBadRequest("Invalid item ID.");
        if (photo == null)
            return this.ApiBadRequest("No photo file was uploaded.");

        if (!await FileValidationHelper.IsValidImageFileAsync(photo))
            return this.ApiBadRequest(
                "Invalid file type or size. Only JPG, PNG, and WEBP files under 5MB are accepted.");

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return this.ApiUnauthorized("Unable to identify user.");

            if (!await _itemService.IsItemOwnerAsync(id, user.Id))
                return this.ApiForbidden("You are not authorized to upload photos for this item.");

            string photoPath;
            using (var stream = photo.OpenReadStream())
            {
                photoPath = await _photoService.UploadPhotoAsync(stream, photo.FileName, photo.ContentType);
            }

            var photoCreateDto = new PhotoCreateDto
            {
                Path = photoPath,
                FileName = Path.GetFileName(photo.FileName),
                FileSize = photo.Length,
                ItemId = id
            };

            var photoId = await _photoService.AddPhotoAsync(photoCreateDto, user.Id);
            var createdPhotoDto = await _photoService.GetPhotoByIdAsync(photoId);

            if (createdPhotoDto == null)
            {
                _logger.LogError("Could not retrieve photo info (ID: {PhotoId}) after upload for item {ItemId}",
                    photoId, id);
                return this.ApiInternalError("Error retrieving uploaded photo information.");
            }

            return this.ApiOk(createdPhotoDto, "Photo uploaded successfully");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbidden("You are not authorized to upload photos for this item.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.ApiNotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for item. ItemId: {ItemId}", id);
            return this.ApiInternalError("Error uploading photo", ex);
        }
    }

    [HttpDelete("{itemId:int}/photos/{photoId:int}")]
    [Authorize(Policy = "ApiAuthPolicy")]
    public async Task<IActionResult> DeletePhoto(int itemId, int photoId)
    {
        if (itemId <= 0 || photoId <= 0) return this.ApiBadRequest("Invalid item or photo ID.");

        try
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) return this.ApiUnauthorized("Unable to identify user.");

            if (!await _itemService.IsItemOwnerAsync(itemId, user.Id))
                return this.ApiForbidden("You are not authorized to delete photos for this item.");

            await _photoService.DeletePhotoAsync(photoId, user.Id);
            return this.ApiOk("Photo deleted successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.ApiNotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbidden("You are not authorized to delete this photo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo. PhotoId: {PhotoId}, ItemId: {ItemId}", photoId, itemId);
            return this.ApiInternalError("Error deleting photo", ex);
        }
    }
    
    
}