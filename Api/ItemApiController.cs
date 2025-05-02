using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using LeafLoop.Models;
using LeafLoop.Models.API;
using LeafLoop.Middleware;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public async Task<ActionResult<ApiResponse<IEnumerable<ItemDto>>>> GetItems([FromQuery] ItemSearchDto searchDto)
        {
            try
            {
                // Set defaults if not provided
                searchDto.Page ??= 1;
                searchDto.PageSize ??= 8;
        
                // Get items based on search criteria
                var items = await _itemService.SearchItemsAsync(searchDto);
        
                // Get total count for pagination
                var totalItems = await _itemService.GetItemsCountAsync(searchDto);
                var totalPages = (int)Math.Ceiling((double)totalItems / searchDto.PageSize.Value);
        
                // Return both items and pagination info using our standardized response
                return this.ApiOkWithPagination(
                    items, totalItems, totalPages, searchDto.Page.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return this.ApiError<IEnumerable<ItemDto>>(
                    StatusCodes.Status500InternalServerError, "Error retrieving items");
            }
        }

        // GET: api/items/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ItemWithDetailsDto>>> GetItem(int id)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest<ItemWithDetailsDto>("Invalid item ID");
            }
            
            try
            {
                var item = await _itemService.GetItemWithDetailsAsync(id);
                
                if (item == null)
                {
                    return this.ApiNotFound<ItemWithDetailsDto>($"Item with ID {id} not found");
                }
                
                return this.ApiOk(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item details. ItemId: {ItemId}", id);
                return this.ApiError<ItemWithDetailsDto>(
                    StatusCodes.Status500InternalServerError, "Error retrieving item details");
            }
        }
        
        // GET: api/items/my
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<IEnumerable<ItemDto>>>> GetCurrentUserItems()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    return this.ApiError<IEnumerable<ItemDto>>(
                        StatusCodes.Status401Unauthorized, "Unable to identify user");
                }

                var items = await _itemService.GetItemsByUserAsync(userId);
                return this.ApiOk(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user items");
                return this.ApiError<IEnumerable<ItemDto>>(
                    StatusCodes.Status500InternalServerError, "Error retrieving your items");
            }
        }

        // POST: api/items
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ItemDto>>> CreateItem([FromBody] ItemCreateDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return this.ApiBadRequest<ItemDto>(
                    "Invalid data. Please check your input and try again.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return this.ApiError<ItemDto>(
                        StatusCodes.Status401Unauthorized, "Unable to identify user");
                }

                var itemId = await _itemService.AddItemAsync(itemDto, user.Id);
                var createdItemDto = await _itemService.GetItemByIdAsync(itemId);
                
                if (createdItemDto == null)
                {
                    _logger.LogError("Could not retrieve the item (ID: {ItemId}) right after creation", itemId);
                    return this.ApiError<ItemDto>(
                        StatusCodes.Status500InternalServerError, "Error retrieving created item");
                }

                return Created($"/api/items/{itemId}", 
                    ApiResponse<ItemDto>.SuccessResponse(createdItemDto, "Item created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item. DTO: {@ItemDto}", itemDto);
                return this.ApiError<ItemDto>(
                    StatusCodes.Status500InternalServerError, "Error creating item");
            }
        }

        // PUT: api/items/{id}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> UpdateItem(int id, [FromBody] ItemUpdateDto itemDto)
        {
            if (id != itemDto.Id)
            {
                return this.ApiBadRequest<object>("Item ID mismatch");
            }
            
            if (!ModelState.IsValid)
            {
                return this.ApiBadRequest<object>(
                    "Invalid data. Please check your input and try again.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return this.ApiError<object>(
                        StatusCodes.Status401Unauthorized, "Unable to identify user");
                }

                await _itemService.UpdateItemAsync(itemDto, user.Id);

                return this.ApiOk<object>(null, "Item updated successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return this.ApiNotFound<object>(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item. ItemId: {ItemId}, DTO: {@ItemDto}", id, itemDto);
                return this.ApiError<object>(
                    StatusCodes.Status500InternalServerError, "Error updating item");
            }
        }

        // DELETE: api/items/{id}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteItem(int id)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest<object>("Invalid item ID");
            }
            
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return this.ApiError<object>(
                        StatusCodes.Status401Unauthorized, "Unable to identify user");
                }

                await _itemService.DeleteItemAsync(id, user.Id);

                return this.ApiOk<object>(null, "Item deleted successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return this.ApiNotFound<object>(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item. ItemId: {ItemId}", id);
                return this.ApiError<object>(
                    StatusCodes.Status500InternalServerError, "Error deleting item");
            }
        }

        // POST: api/items/{id}/photos
        [HttpPost("{id:int}/photos")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PhotoDto>>> UploadPhoto(int id, IFormFile photo)
        {
            if (id <= 0) 
            {
                return this.ApiBadRequest<PhotoDto>("Invalid item ID");
            }
            
            if (photo == null || photo.Length == 0) 
            {
                return this.ApiBadRequest<PhotoDto>("No photo file was uploaded");
            }
            
            if (photo.Length > 5 * 1024 * 1024) 
            {
                return this.ApiBadRequest<PhotoDto>("File size exceeds maximum of 5MB");
            }
            
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(photo.ContentType.ToLowerInvariant())) 
            {
                return this.ApiBadRequest<PhotoDto>("Invalid file type (allowed: JPG, PNG, WEBP)");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return this.ApiError<PhotoDto>(
                        StatusCodes.Status401Unauthorized, "Unable to identify user");
                }

                // Check if user owns the item
                if (!await _itemService.IsItemOwnerAsync(id, user.Id))
                {
                    return Forbid();
                }

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
                    return this.ApiError<PhotoDto>(
                        StatusCodes.Status500InternalServerError, "Error retrieving uploaded photo information");
                }
                
                return StatusCode(StatusCodes.Status201Created, 
                    ApiResponse<PhotoDto>.SuccessResponse(createdPhotoDto, "Photo uploaded successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return this.ApiNotFound<PhotoDto>(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for item. ItemId: {ItemId}", id);
                return this.ApiError<PhotoDto>(
                    StatusCodes.Status500InternalServerError, "Error uploading photo");
            }
        }

        // DELETE: api/items/{itemId}/photos/{photoId}
        [HttpDelete("{itemId:int}/photos/{photoId:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeletePhoto(int itemId, int photoId)
        {
            if (itemId <= 0 || photoId <= 0)
            {
                return this.ApiBadRequest<object>("Invalid item or photo ID");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return this.ApiError<object>(
                        StatusCodes.Status401Unauthorized, "Unable to identify user");
                }

                // Check if user owns the item
                if (!await _itemService.IsItemOwnerAsync(itemId, user.Id))
                {
                    return Forbid();
                }

                await _photoService.DeletePhotoAsync(photoId, user.Id);

                return this.ApiOk<object>(null, "Photo deleted successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return this.ApiNotFound<object>(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo. PhotoId: {PhotoId}, ItemId: {ItemId}", photoId, itemId);
                return this.ApiError<object>(
                    StatusCodes.Status500InternalServerError, "Error deleting photo");
            }
        }
    }
}