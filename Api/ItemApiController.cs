using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse and ApiResponse<T>
using LeafLoop.Services.DTOs; // For Item DTOs, PhotoCreateDto, etc.
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // For StatusCodes, IFormFile
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // For IEnumerable
using System.IO; // For Path.GetFileName
using System.Linq; // For Count()
using System.Security.Claims; // For ClaimTypes
using System.Threading.Tasks; // For Task
// Assuming FileValidationHelper is in a namespace like LeafLoop.Helpers or LeafLoop.Utils
// using LeafLoop.Helpers; 

namespace LeafLoop.Api
{
    /// <summary>
    /// Manages items (listings) in the application.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
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

        /// <summary>
        /// Retrieves a paginated list of items based on search criteria.
        /// </summary>
        /// <param name="searchDto">The search parameters for filtering and pagination.</param>
        /// <returns>A paginated list of items.</returns>
        /// <response code="200">Returns the paginated list of items.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [AllowAnonymous] // Assuming item search is public
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItems([FromQuery] ItemSearchDto searchDto)
        {
            _logger.LogInformation("API GetItems START. SearchDto: {@SearchDto}", searchDto);
            try
            {
                searchDto.Page ??= 1;
                searchDto.PageSize ??= 8; 

                if (searchDto.Page <= 0) searchDto.Page = 1;
                if (searchDto.PageSize <= 0 || searchDto.PageSize > 100) searchDto.PageSize = 8; 

                var items = await _itemService.SearchItemsAsync(searchDto);
                var totalItems = await _itemService.GetItemsCountAsync(searchDto); 
                var totalPages = (totalItems == 0) ? 0 : (int)Math.Ceiling((double)totalItems / searchDto.PageSize.Value);

                _logger.LogInformation("API GetItems SUCCESS. Page: {Page}, PageSize: {PageSize}, TotalItems: {TotalItems}, TotalPages: {TotalPages}",
                    searchDto.Page, searchDto.PageSize, totalItems, totalPages);
                return this.ApiOkWithPagination(items, totalItems, totalPages, searchDto.Page.Value, "Items retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetItems ERROR. SearchDto: {@SearchDto}", searchDto);
                return this.ApiInternalError("Error retrieving items.", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific item by its ID, including details.
        /// </summary>
        /// <param name="id">The ID of the item to retrieve.</param>
        /// <returns>The item with the specified ID.</returns>
        /// <response code="200">Returns the requested item details.</response>
        /// <response code="400">If the item ID is invalid.</response>
        /// <response code="404">If the item with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}", Name = "GetItemById")]
        [AllowAnonymous] 
        [ProducesResponseType(typeof(ApiResponse<ItemWithDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItem(int id)
        {
            _logger.LogInformation("API GetItem START for ID: {ItemId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("API GetItem BAD_REQUEST: Invalid item ID: {ItemId}", id);
                return this.ApiBadRequest("Invalid item ID.");
            }

            try
            {
                var item = await _itemService.GetItemWithDetailsAsync(id);
                if (item == null)
                {
                    _logger.LogWarning("API GetItem NOT_FOUND: Item with ID {ItemId} not found.", id);
                    return this.ApiNotFound($"Item with ID {id} not found.");
                }
                _logger.LogInformation("API GetItem SUCCESS for ID: {ItemId}", id);
                return this.ApiOk(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetItem ERROR for ID: {ItemId}", id);
                return this.ApiInternalError("Error retrieving item details.", ex);
            }
        }

        /// <summary>
        /// Retrieves items listed by the currently authenticated user.
        /// </summary>
        /// <returns>A list of items owned by the current user.</returns>
        /// <response code="200">Returns the list of user's items.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("my")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrentUserItems()
        {
            _logger.LogInformation("API GetCurrentUserItems START for User: {UserName}", User.Identity?.Name ?? "N/A");
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    _logger.LogWarning("API GetCurrentUserItems UNAUTHORIZED: Unable to identify user from claims. User: {UserName}", User.Identity?.Name);
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                var items = await _itemService.GetItemsByUserAsync(userId);
                _logger.LogInformation("API GetCurrentUserItems SUCCESS for UserID: {UserId}. Item count: {Count}", userId, items?.Count() ?? 0);
                return this.ApiOk(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetCurrentUserItems ERROR for User: {UserName}", User.Identity?.Name);
                return this.ApiInternalError("Error retrieving your items.", ex);
            }
        }

        /// <summary>
        /// Creates a new item. Requires authentication.
        /// </summary>
        /// <param name="itemDto">The data for the new item.</param>
        /// <returns>The newly created item.</returns>
        /// <response code="201">Returns the newly created item and its location.</response>
        /// <response code="400">If the item data is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<ItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateItem([FromBody] ItemCreateDto itemDto)
        {
            _logger.LogInformation("API CreateItem START by User: {UserName}, Item Name: {ItemName}", User.Identity?.Name ?? "N/A", itemDto?.Name ?? "N/A");
            if (!ModelState.IsValid)
            {
                 _logger.LogWarning("API CreateItem BAD_REQUEST: Invalid model state by User: {UserName}. Errors: {@ModelStateErrors}", User.Identity?.Name, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API CreateItem UNAUTHORIZED: User not found though endpoint is authorized.");
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                var itemId = await _itemService.AddItemAsync(itemDto, user.Id);
                var createdItemDto = await _itemService.GetItemByIdAsync(itemId); 

                if (createdItemDto == null)
                {
                    _logger.LogError("API CreateItem ERROR: Could not retrieve item (ID: {ItemId}) immediately after creation by UserID: {UserId}", itemId, user.Id);
                    return this.ApiInternalError("Error retrieving created item details.");
                }
                _logger.LogInformation("API CreateItem SUCCESS. New ItemID: {ItemId}, Name: {ItemName}, UserID: {UserId}", itemId, createdItemDto.Name, user.Id);
                return this.ApiCreatedAtAction(
                    createdItemDto,
                    nameof(GetItem), 
                    this.ControllerContext.ActionDescriptor.ControllerName, 
                    new { id = itemId }, 
                    "Item created successfully."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API CreateItem ERROR by User: {UserName}. DTO: {@ItemDto}", User.Identity?.Name, itemDto);
                return this.ApiInternalError("Error creating item.", ex);
            }
        }

        /// <summary>
        /// Updates an existing item. Requires authentication and user must be the owner.
        /// </summary>
        /// <param name="id">The ID of the item to update.</param>
        /// <param name="itemDto">The updated item data.</param>
        /// <returns>A 200 OK response with a success message, or 204 No Content if preferred.</returns>
        /// <response code="200">Item updated successfully (returns a message).</response>
        /// <response code="400">If the item data is invalid or ID mismatch.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to update this item.</response>
        /// <response code="404">If the item with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] 
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateDto itemDto)
        {
            _logger.LogInformation("API UpdateItem START for ItemID: {ItemId} by User: {UserName}", id, User.Identity?.Name ?? "N/A");
            if (id != itemDto.Id)
            {
                _logger.LogWarning("API UpdateItem BAD_REQUEST: Item ID mismatch in URL ({UrlId}) and body ({BodyId}) by User: {UserName}.", id, itemDto.Id, User.Identity?.Name);
                return this.ApiBadRequest("Item ID mismatch in URL and body.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateItem BAD_REQUEST: Invalid model state for ItemID {ItemId} by User: {UserName}. Errors: {@ModelStateErrors}", id, User.Identity?.Name, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API UpdateItem UNAUTHORIZED: User not found though endpoint is authorized. ItemID: {ItemId}", id);
                    return this.ApiUnauthorized("Unable to identify user.");
                }
                await _itemService.UpdateItemAsync(itemDto, user.Id);
                _logger.LogInformation("API UpdateItem SUCCESS for ItemID: {ItemId} by UserID: {UserId}", id, user.Id);
                return this.ApiOk("Item updated successfully."); 
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "API UpdateItem NOT_FOUND: ItemID {ItemId} not found for update by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiNotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("API UpdateItem FORBIDDEN: User {UserName} is not authorized to update ItemID {ItemId}.", User.Identity?.Name, id);
                return this.ApiForbidden("You are not authorized to update this item.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateItem ERROR for ItemID: {ItemId} by User: {UserName}. DTO: {@ItemDto}", id, User.Identity?.Name, itemDto);
                return this.ApiInternalError("Error updating item.", ex);
            }
        }

        /// <summary>
        /// Deletes an item. Requires authentication and user must be the owner.
        /// </summary>
        /// <param name="id">The ID of the item to delete.</param>
        /// <returns>A 200 OK response with a success message, or 204 No Content if preferred.</returns>
        /// <response code="200">Item deleted successfully (returns a message).</response>
        /// <response code="400">If the item ID is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to delete this item.</response>
        /// <response code="404">If the item with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] 
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteItem(int id)
        {
            _logger.LogInformation("API DeleteItem START for ItemID: {ItemId} by User: {UserName}", id, User.Identity?.Name ?? "N/A");
            if (id <= 0)
            {
                _logger.LogWarning("API DeleteItem BAD_REQUEST: Invalid item ID: {ItemId} by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiBadRequest("Invalid item ID.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API DeleteItem UNAUTHORIZED: User not found though endpoint is authorized. ItemID: {ItemId}", id);
                    return this.ApiUnauthorized("Unable to identify user.");
                }
                await _itemService.DeleteItemAsync(id, user.Id);
                _logger.LogInformation("API DeleteItem SUCCESS for ItemID: {ItemId} by UserID: {UserId}", id, user.Id);
                return this.ApiOk("Item deleted successfully."); 
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "API DeleteItem NOT_FOUND: ItemID {ItemId} not found for deletion by User: {UserName}.", id, User.Identity?.Name);
                return this.ApiNotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("API DeleteItem FORBIDDEN: User {UserName} is not authorized to delete ItemID {ItemId}.", User.Identity?.Name, id);
                return this.ApiForbidden("You are not authorized to delete this item.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API DeleteItem ERROR for ItemID: {ItemId} by User: {UserName}", id, User.Identity?.Name);
                return this.ApiInternalError("Error deleting item.", ex);
            }
        }

        /// <summary>
        /// Uploads a photo for a specific item. Requires authentication and user must be the owner of the item.
        /// </summary>
        /// <param name="id">The ID of the item to associate the photo with.</param>
        /// <param name="photo">The photo file to upload. Must be a valid image (JPG, PNG, WEBP) and under 5MB.</param>
        /// <returns>Details of the uploaded photo.</returns>
        /// <response code="200">Photo uploaded successfully. Returns photo details.</response>
        /// <response code="400">If item ID is invalid, no photo is provided, or photo is invalid (type/size).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to upload photos for this item.</response>
        /// <response code="404">If the item with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/photos")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [Consumes("multipart/form-data")] 
        [ProducesResponseType(typeof(ApiResponse<PhotoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile photo) // Removed [FromForm] here
        {
            _logger.LogInformation("API UploadPhoto START for ItemID: {ItemId} by User: {UserName}. File: {FileName}, Size: {FileSize}",
                id, User.Identity?.Name ?? "N/A", photo?.FileName, photo?.Length);

            if (id <= 0)
            {
                 _logger.LogWarning("API UploadPhoto BAD_REQUEST: Invalid item ID: {ItemId}", id);
                return this.ApiBadRequest("Invalid item ID.");
            }
            if (photo == null || photo.Length == 0)
            {
                _logger.LogWarning("API UploadPhoto BAD_REQUEST: No photo file uploaded for ItemID: {ItemId}", id);
                return this.ApiBadRequest("No photo file was uploaded.");
            }

            long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (photo.Length > maxFileSize)
            {
                 _logger.LogWarning("API UploadPhoto BAD_REQUEST: File size {FileSize} exceeds limit for ItemID: {ItemId}", photo.Length, id);
                return this.ApiBadRequest($"File size exceeds limit of {maxFileSize / 1024 / 1024} MB.");
            }
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("API UploadPhoto BAD_REQUEST: Invalid file type '{ContentType}' for ItemID: {ItemId}", photo.ContentType, id);
                return this.ApiBadRequest("Invalid file type. Only JPG, PNG, and WEBP are accepted.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API UploadPhoto UNAUTHORIZED: User not found for ItemID: {ItemId}", id);
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                if (!await _itemService.IsItemOwnerAsync(id, user.Id))
                {
                    _logger.LogWarning("API UploadPhoto FORBIDDEN: User {UserId} is not owner of ItemID {ItemId}.", user.Id, id);
                    return this.ApiForbidden("You are not authorized to upload photos for this item.");
                }

                string photoPath;
                using (var stream = photo.OpenReadStream())
                {
                    photoPath = await _photoService.UploadPhotoAsync(stream, photo.FileName, photo.ContentType, "item_photos");
                }
                _logger.LogInformation("Photo uploaded to temporary path: {PhotoPath} for ItemID: {ItemId}", photoPath, id);

                var photoCreateDto = new PhotoCreateDto
                {
                    Path = photoPath, 
                    FileName = Path.GetFileName(photo.FileName), 
                    FileSize = photo.Length,
                    ItemId = id,
                };

                var photoId = await _photoService.AddPhotoAsync(photoCreateDto, user.Id); 
                var createdPhotoDto = await _photoService.GetPhotoByIdAsync(photoId);

                if (createdPhotoDto == null)
                {
                    _logger.LogError("API UploadPhoto ERROR: Could not retrieve photo info (ID: {PhotoId}) after DB record creation for ItemID: {ItemId}", photoId, id);
                    await _photoService.DeletePhotoByPathAsync(photoPath); // Attempt to delete orphaned file
                    return this.ApiInternalError("Error retrieving uploaded photo information after saving.");
                }
                _logger.LogInformation("API UploadPhoto SUCCESS for ItemID: {ItemId}. New PhotoID: {PhotoId}, Path: {Path}", id, photoId, createdPhotoDto.Path);
                return this.ApiOk(createdPhotoDto, "Photo uploaded successfully.");
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API UploadPhoto NOT_FOUND: ItemID {ItemId} not found. User: {UserName}", id, User.Identity?.Name);
                return this.ApiNotFound(ex.Message); 
            }
            catch (UnauthorizedAccessException) 
            {
                _logger.LogWarning("API UploadPhoto FORBIDDEN (service level): User {UserName} for ItemID {ItemId}.", User.Identity?.Name, id);
                return this.ApiForbidden("You are not authorized to upload photos for this item.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UploadPhoto ERROR for ItemID: {ItemId}. User: {UserName}", id, User.Identity?.Name);
                return this.ApiInternalError("Error uploading photo.", ex);
            }
        }

        /// <summary>
        /// Deletes a photo associated with an item. Requires authentication and user must be the owner of the item.
        /// </summary>
        /// <param name="itemId">The ID of the item the photo belongs to.</param>
        /// <param name="photoId">The ID of the photo to delete.</param>
        /// <returns>A 200 OK response with a success message, or 204 No Content if preferred.</returns>
        /// <response code="200">Photo deleted successfully (returns a message).</response>
        /// <response code="400">If item or photo ID is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to delete this photo.</response>
        /// <response code="404">If the item or photo is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpDelete("{itemId:int}/photos/{photoId:int}")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] 
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePhoto(int itemId, int photoId)
        {
             _logger.LogInformation("API DeletePhoto START for ItemID: {ItemId}, PhotoID: {PhotoId} by User: {UserName}",
                itemId, photoId, User.Identity?.Name ?? "N/A");

            if (itemId <= 0 || photoId <= 0)
            {
                _logger.LogWarning("API DeletePhoto BAD_REQUEST: Invalid ItemID ({ItemId}) or PhotoID ({PhotoId}).", itemId, photoId);
                return this.ApiBadRequest("Invalid item or photo ID.");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API DeletePhoto UNAUTHORIZED: User not found for ItemID: {ItemId}, PhotoID: {PhotoId}", itemId, photoId);
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                if (!await _itemService.IsItemOwnerAsync(itemId, user.Id)) 
                {
                     _logger.LogWarning("API DeletePhoto FORBIDDEN: User {UserId} is not owner of ItemID {ItemId}.", user.Id, itemId);
                    return this.ApiForbidden("You are not authorized to modify photos for this item.");
                }
                // The PhotoService.DeletePhotoAsync should verify that the photoId belongs to the itemId and handle file deletion.
                await _photoService.DeletePhotoAsync(photoId, user.Id); 
                _logger.LogInformation("API DeletePhoto SUCCESS for ItemID: {ItemId}, PhotoID: {PhotoId} by UserID: {UserId}", itemId, photoId, user.Id);
                return this.ApiOk("Photo deleted successfully."); 
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API DeletePhoto NOT_FOUND: ItemID {ItemId} or PhotoID {PhotoId} not found. User: {UserName}", itemId, photoId, User.Identity?.Name);
                return this.ApiNotFound(ex.Message); 
            }
            catch (UnauthorizedAccessException)
            {
                 _logger.LogWarning("API DeletePhoto FORBIDDEN (service level): User {UserName} for ItemID {ItemId}, PhotoID: {PhotoId}.", User.Identity?.Name, itemId, photoId);
                return this.ApiForbidden("You are not authorized to delete this photo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API DeletePhoto ERROR for ItemID: {ItemId}, PhotoID: {PhotoId}. User: {UserName}", itemId, photoId, User.Identity?.Name);
                return this.ApiInternalError("Error deleting photo.", ex);
            }
        }
    }
}
