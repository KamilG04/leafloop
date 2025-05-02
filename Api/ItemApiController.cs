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
                return Ok(ApiResponse<IEnumerable<ItemDto>>.SuccessResponse(
                    items, totalItems, totalPages, searchDto.Page.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<IEnumerable<ItemDto>>.ErrorResponse("Error retrieving items"));
            }
        }

        // GET: api/items/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ItemWithDetailsDto>>> GetItem(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponse<ItemWithDetailsDto>.ErrorResponse("Invalid item ID"));
            }
            
            try
            {
                var item = await _itemService.GetItemWithDetailsAsync(id);
                
                if (item == null)
                {
                    return NotFound(ApiResponse<ItemWithDetailsDto>.ErrorResponse($"Item with ID {id} not found"));
                }
                
                return Ok(ApiResponse<ItemWithDetailsDto>.SuccessResponse(item));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item details. ItemId: {ItemId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<ItemWithDetailsDto>.ErrorResponse("Error retrieving item details"));
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
                    return Unauthorized(ApiResponse<IEnumerable<ItemDto>>.ErrorResponse("Unable to identify user"));
                }

                var items = await _itemService.GetItemsByUserAsync(userId);
                return Ok(ApiResponse<IEnumerable<ItemDto>>.SuccessResponse(items));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user items");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<IEnumerable<ItemDto>>.ErrorResponse("Error retrieving your items"));
            }
        }

        // POST: api/items
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ItemDto>>> CreateItem([FromBody] ItemCreateDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ItemDto>.ErrorResponse(
                    "Invalid data. Please check your input and try again."));
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return Unauthorized(ApiResponse<ItemDto>.ErrorResponse("Unable to identify user"));
                }

                var itemId = await _itemService.AddItemAsync(itemDto, user.Id);
                var createdItemDto = await _itemService.GetItemByIdAsync(itemId);
                
                if (createdItemDto == null)
                {
                    _logger.LogError("Could not retrieve the item (ID: {ItemId}) right after creation", itemId);
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        ApiResponse<ItemDto>.ErrorResponse("Error retrieving created item"));
                }

                return CreatedAtAction(nameof(GetItem), new { id = itemId }, 
                    ApiResponse<ItemDto>.SuccessResponse(createdItemDto, "Item created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item. DTO: {@ItemDto}", itemDto);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<ItemDto>.ErrorResponse("Error creating item"));
            }
        }

        // PUT: api/items/{id}
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> UpdateItem(int id, [FromBody] ItemUpdateDto itemDto)
        {
            if (id != itemDto.Id)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Item ID mismatch"));
            }
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid data. Please check your input and try again."));
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unable to identify user"));
                }

                await _itemService.UpdateItemAsync(itemDto, user.Id);

                return Ok(ApiResponse<object>.SuccessResponse(null, "Item updated successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item. ItemId: {ItemId}, DTO: {@ItemDto}", id, itemDto);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<object>.ErrorResponse("Error updating item"));
            }
        }

        // DELETE: api/items/{id}
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteItem(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid item ID"));
            }
            
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unable to identify user"));
                }

                await _itemService.DeleteItemAsync(id, user.Id);

                return Ok(ApiResponse<object>.SuccessResponse(null, "Item deleted successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item. ItemId: {ItemId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<object>.ErrorResponse("Error deleting item"));
            }
        }

        // POST: api/items/{id}/photos
        [HttpPost("{id:int}/photos")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PhotoDto>>> UploadPhoto(int id, IFormFile photo)
        {
            if (id <= 0) 
            {
                return BadRequest(ApiResponse<PhotoDto>.ErrorResponse("Invalid item ID"));
            }
            
            if (photo == null || photo.Length == 0) 
            {
                return BadRequest(ApiResponse<PhotoDto>.ErrorResponse("No photo file was uploaded"));
            }
            
            if (photo.Length > 5 * 1024 * 1024) 
            {
                return BadRequest(ApiResponse<PhotoDto>.ErrorResponse("File size exceeds maximum of 5MB"));
            }
            
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(photo.ContentType.ToLowerInvariant())) 
            {
                return BadRequest(ApiResponse<PhotoDto>.ErrorResponse("Invalid file type (allowed: JPG, PNG, WEBP)"));
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return Unauthorized(ApiResponse<PhotoDto>.ErrorResponse("Unable to identify user"));
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
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        ApiResponse<PhotoDto>.ErrorResponse("Error retrieving uploaded photo information"));
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
                return NotFound(ApiResponse<PhotoDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for item. ItemId: {ItemId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<PhotoDto>.ErrorResponse("Error uploading photo"));
            }
        }

        // DELETE: api/items/{itemId}/photos/{photoId}
        [HttpDelete("{itemId:int}/photos/{photoId:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeletePhoto(int itemId, int photoId)
        {
            if (itemId <= 0 || photoId <= 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid item or photo ID"));
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                if (user == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unable to identify user"));
                }

                // Check if user owns the item
                if (!await _itemService.IsItemOwnerAsync(itemId, user.Id))
                {
                    return Forbid();
                }

                await _photoService.DeletePhotoAsync(photoId, user.Id);

                return Ok(ApiResponse<object>.SuccessResponse(null, "Photo deleted successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo. PhotoId: {PhotoId}, ItemId: {ItemId}", photoId, itemId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    ApiResponse<object>.ErrorResponse("Error deleting photo"));
            }
        }
    }
}