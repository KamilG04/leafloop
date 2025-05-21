using System;
using System.Linq; // Required for .Count() on IEnumerable
using System.Security.Claims; // Required for ClaimTypes
using System.Threading.Tasks;
using LeafLoop.Models; // Required for User
using LeafLoop.Models.API;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity; // Required for UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Api
{
    /// <summary>
    /// Controller for handling searches for items near a specific location.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class NearbyItemsController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly UserManager<User> _userManager; // Added for GetItemsNearCurrentUser
        private readonly ILogger<NearbyItemsController> _logger;

        /// <summary>
        /// Initializes a new instance of the NearbyItemsController.
        /// </summary>
        /// <param name="itemService">The item service.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger.</param>
        public NearbyItemsController(
            IItemService itemService,
            UserManager<User> userManager, // Added
            ILogger<NearbyItemsController> logger)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager)); // Added
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a list of items near the specified location.
        /// </summary>
        /// <param name="lat">Latitude of the center point.</param>
        /// <param name="lon">Longitude of the center point.</param>
        /// <param name="radius">Search radius in kilometers (default: 10 km).</param>
        /// <param name="categoryId">Optional category ID to filter by.</param>
        /// <param name="searchTerm">Optional search term.</param>
        /// <param name="page">Page number (1-based, default: 1).</param>
        /// <param name="pageSize">Page size (default: 20, max: 100).</param>
        /// <returns>A paginated list of items found near the location.</returns>
        /// <response code="200">Returns the list of nearby items.</response>
        /// <response code="400">If the request parameters are invalid.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ItemSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNearbyItems(
            [FromQuery] decimal lat,
            [FromQuery] decimal lon,
            [FromQuery] decimal radius = 10,
            [FromQuery] int? categoryId = null,
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation(
                "API GetNearbyItems START. Lat: {Latitude}, Lon: {Longitude}, Radius: {Radius}km, Category: {CategoryId}, Search: {SearchTerm}, Page: {Page}, PageSize: {PageSize}",
                lat, lon, radius, categoryId ?? 0, searchTerm ?? "N/A", page, pageSize);

            // Validate parameters
            if (lat < -90 || lat > 90)
            {
                _logger.LogWarning("API GetNearbyItems BAD_REQUEST: Latitude out of range: {Latitude}", lat);
                return this.ApiBadRequest("Latitude must be between -90 and 90 degrees.");
            }

            if (lon < -180 || lon > 180)
            {
                _logger.LogWarning("API GetNearbyItems BAD_REQUEST: Longitude out of range: {Longitude}", lon);
                return this.ApiBadRequest("Longitude must be between -180 and 180 degrees.");
            }

            if (radius <= 0 || radius > 200) // Max radius of 200km
            {
                _logger.LogWarning("API GetNearbyItems BAD_REQUEST: Invalid radius: {Radius}km", radius);
                return this.ApiBadRequest("Search radius must be greater than 0 and no more than 200 km.");
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20; // Max page size 100, default 20

            try
            {
                var result = await _itemService.GetItemsNearLocationAsync(
                    lat, lon, radius, categoryId, searchTerm, page, pageSize);

                _logger.LogInformation(
                    "API GetNearbyItems SUCCESS. Found: {TotalCount} items, Returned: {ReturnedCount} on page {Page}",
                    result.TotalCount, result.Items?.Count() ?? 0, page);

                return this.ApiOk(result); // Assumes ApiOk can handle PagedResult
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "API GetNearbyItems ERROR. Parameters: Lat: {Latitude}, Lon: {Longitude}, Radius: {Radius}km",
                    lat, lon, radius);
                return this.ApiInternalError("An error occurred while searching for nearby items.", ex);
            }
        }

        /// <summary>
        /// Retrieves a list of items near the currently authenticated user's location.
        /// </summary>
        /// <param name="radius">Optional search radius in kilometers. If not provided, the user's saved search radius is used.</param>
        /// <param name="categoryId">Optional category ID to filter by.</param>
        /// <param name="searchTerm">Optional search term.</param>
        /// <param name="page">Page number (1-based, default: 1).</param>
        /// <param name="pageSize">Page size (default: 20, max: 100).</param>
        /// <returns>A paginated list of items found near the user's location.</returns>
        /// <response code="200">Returns the list of nearby items.</response>
        /// <response code="400">If the user has not set their location, or if request parameters are invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("my-area")]
        [Authorize(Policy = "ApiAuthPolicy")] // Requires authentication
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ItemSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsNearCurrentUser(
            [FromQuery] decimal? radius = null, // Nullable to allow using user's default
            [FromQuery] int? categoryId = null,
            [FromQuery] string searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation(
                "API GetItemsNearCurrentUser START for UserIDClaim: {UserIdClaim}. CustomRadius: {CustomRadius}, Category: {CategoryId}, Search: {SearchTerm}, Page: {Page}, PageSize: {PageSize}",
                userIdClaim ?? "N/A", radius.HasValue ? radius.Value.ToString() : "UserDefault", categoryId ?? 0, searchTerm ?? "N/A", page, pageSize);

            try
            {
                var currentUser = await _userManager.Users
                                          .Include(u => u.Address) // Eager load the Address
                                          .FirstOrDefaultAsync(u => u.Id.ToString() == userIdClaim);

                if (currentUser == null)
                {
                    _logger.LogWarning("API GetItemsNearCurrentUser UNAUTHORIZED: Could not identify current user from IDClaim: {UserIdClaim}.", userIdClaim);
                    return this.ApiUnauthorized("Could not identify current user.");
                }

                if (currentUser.Address == null || !currentUser.Address.Latitude.HasValue || !currentUser.Address.Longitude.HasValue)
                {
                    _logger.LogWarning("API GetItemsNearCurrentUser BAD_REQUEST: User {UserId} has not set their location.", currentUser.Id);
                    return this.ApiBadRequest("To use this feature, you must first set your location in your profile.");
                }

                // Use provided radius or fallback to user's saved search radius
                // Assumes User model has SearchRadius property (e.g., public decimal SearchRadius { get; set; })
                var searchRadiusToUse = radius ?? currentUser.SearchRadius; 

                if (searchRadiusToUse <= 0 || searchRadiusToUse > 200)
                {
                    _logger.LogWarning("API GetItemsNearCurrentUser BAD_REQUEST: Invalid effective search radius: {Radius}km for UserID: {UserId}", searchRadiusToUse, currentUser.Id);
                    return this.ApiBadRequest("Search radius must be greater than 0 and no more than 200 km.");
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _itemService.GetItemsNearLocationAsync(
                    currentUser.Address.Latitude.Value,
                    currentUser.Address.Longitude.Value,
                    searchRadiusToUse,
                    categoryId,
                    searchTerm,
                    page,
                    pageSize);

                _logger.LogInformation(
                    "API GetItemsNearCurrentUser SUCCESS for UserID: {UserId}. Found: {TotalCount} items, Returned: {ReturnedCount} on page {Page}",
                    currentUser.Id, result.TotalCount, result.Items?.Count() ?? 0, page);

                return this.ApiOk(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetItemsNearCurrentUser ERROR for UserIDClaim: {UserIdClaim}", userIdClaim);
                return this.ApiInternalError("An error occurred while searching for items in your area.", ex);
            }
        }
    }
}