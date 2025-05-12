using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse and ApiResponse<T>
using LeafLoop.Services.DTOs; // For User DTOs, AddressDto
using LeafLoop.Services.DTOs.Auth; // For PasswordChangeDto
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // For StatusCodes, IFormFile
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // For IEnumerable
using System.IO; // For Path.GetFileName
using System.Linq; // For Count() and ModelState error logging
using System.Security.Claims; // For ClaimTypes
using System.Threading.Tasks; // For Task
// Assuming FileValidationHelper might be used, though not explicitly in this version's UploadAvatar
// using LeafLoop.Helpers; 

namespace LeafLoop.Api
{
    /// <summary>
    /// Manages user profiles, addresses, avatars, and related user data.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly IPhotoService _photoService; 
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            UserManager<User> userManager,
            IPhotoService photoService,
            ILogger<UsersController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves detailed information for a specific user by their ID.
        /// Requires authentication. The authenticated user can view their own details, or an Admin can view any user's details.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>The detailed information for the specified user.</returns>
        /// <response code="200">Successfully retrieved user details.</response>
        /// <response code="400">If the user ID is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the authenticated user is not authorized to view this user's details.</response>
        /// <response code="404">If the user with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}", Name = "GetUserById")] 
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<UserWithDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUser(int id)
        {
            var authUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API GetUser START for RequestedID: {RequestedId}, AuthenticatedUserIDClaim: {AuthUserIdClaim}", 
                id, authUserIdClaim ?? "N/A");

            if (id <= 0)
            {
                _logger.LogWarning("API GetUser BAD_REQUEST: Invalid User ID: {RequestedId}", id);
                return this.ApiBadRequest("Invalid User ID.");
            }
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("API GetUser UNAUTHORIZED: Current user could not be identified from token. RequestedID: {RequestedId}", id);
                    return this.ApiUnauthorized("Could not identify current user.");
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                if (currentUser.Id != id && !isAdmin)
                {
                    _logger.LogWarning("API GetUser FORBIDDEN: User {CurrentUserId} is not authorized to view details for UserID {RequestedId}.", currentUser.Id, id);
                    return this.ApiForbidden("You are not authorized to view this user's details.");
                }

                var userDto = await _userService.GetUserWithDetailsAsync(id);
                if (userDto == null)
                {
                    _logger.LogWarning("API GetUser NOT_FOUND: User with ID {RequestedId} not found.", id);
                    return this.ApiNotFound($"User with ID {id} not found.");
                }
                _logger.LogInformation("API GetUser SUCCESS for RequestedID: {RequestedId}", id);
                return this.ApiOk(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUser ERROR for RequestedID: {RequestedId}", id);
                return this.ApiInternalError("Error retrieving user data.", ex);
            }
        }

        /// <summary>
        /// Retrieves basic information for the currently authenticated user.
        /// </summary>
        /// <returns>The basic information for the current user.</returns>
        /// <response code="200">Successfully retrieved current user's details.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="404">If the current user's details could not be found (should be rare if authenticated).</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("current")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API GetCurrentUser START for UserID Claim: {UserIdClaim}", userIdClaim ?? "N/A");
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("API GetCurrentUser UNAUTHORIZED: Current user could not be identified from token. UserID Claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("Current user could not be identified.");
                }

                var userDto = await _userService.GetUserByIdAsync(currentUser.Id);
                if (userDto == null)
                {
                    _logger.LogWarning("API GetCurrentUser NOT_FOUND: Could not find user details for authenticated UserID {UserId} (Claim: {UserIdClaim}).", 
                        currentUser.Id, userIdClaim ?? "N/A");
                    return this.ApiNotFound("Current user details could not be found.");
                }
                _logger.LogInformation("API GetCurrentUser SUCCESS for UserID: {UserId}", currentUser.Id);
                return this.ApiOk(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetCurrentUser ERROR for UserID Claim: {UserIdClaim}", userIdClaim ?? "N/A");
                return this.ApiInternalError("Error retrieving current user data.", ex);
            }
        }

        /// <summary>
        /// Updates the profile information (e.g., name) for a specific user.
        /// Requires authentication. The authenticated user can update their own profile, or an Admin can update any user's profile.
        /// </summary>
        /// <param name="id">The ID of the user whose profile is to be updated.</param>
        /// <param name="userUpdateDto">The DTO containing the updated profile information.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">User profile updated successfully.</response>
        /// <response code="400">If the user ID in the URL does not match the ID in the DTO, or if the DTO is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the authenticated user is not authorized to update this profile.</response>
        /// <response code="404">If the user with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserProfile(int id, [FromBody] UserUpdateDto userUpdateDto)
        {
            var authUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API UpdateUserProfile START for TargetUserID: {TargetUserId}, AuthenticatedUserIDClaim: {AuthUserIdClaim}. DTO: {@UserUpdateDto}", 
                id, authUserIdClaim ?? "N/A", userUpdateDto);

            if (id != userUpdateDto.Id)
            {
                _logger.LogWarning("API UpdateUserProfile BAD_REQUEST: User ID mismatch in URL ({UrlId}) and body ({BodyId}).", id, userUpdateDto.Id);
                return this.ApiBadRequest("User ID mismatch in URL and body.");
            }
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateUserProfile BAD_REQUEST: Invalid model state for TargetUserID {TargetUserId}. Errors: {@ModelStateErrors}", 
                    id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("API UpdateUserProfile UNAUTHORIZED: Current user could not be identified. TargetUserID: {TargetUserId}", id);
                    return this.ApiUnauthorized("Could not identify current user.");
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                if (currentUser.Id != id && !isAdmin)
                {
                     _logger.LogWarning("API UpdateUserProfile FORBIDDEN: User {CurrentUserId} is not authorized to update profile for UserID {TargetUserId}.", 
                        currentUser.Id, id);
                    return this.ApiForbidden("You are not authorized to update this profile.");
                }

                await _userService.UpdateUserProfileAsync(userUpdateDto);
                _logger.LogInformation("API UpdateUserProfile SUCCESS for TargetUserID: {TargetUserId}", id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "API UpdateUserProfile NOT_FOUND: User with ID {TargetUserId} not found.", id);
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (UnauthorizedAccessException ex) 
            {
                _logger.LogWarning(ex, "API UpdateUserProfile FORBIDDEN (service level) for TargetUserID: {TargetUserId}.", id);
                return this.ApiForbidden("Authorization error during update.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateUserProfile ERROR for TargetUserID: {TargetUserId}. DTO: {@UserUpdateDto}", id, userUpdateDto);
                return this.ApiInternalError("Error updating user profile.", ex);
            }
        }

        /// <summary>
        /// Updates the address for a specific user.
        /// Requires authentication. The authenticated user can update their own address, or an Admin can update any user's address.
        /// </summary>
        /// <param name="id">The ID of the user whose address is to be updated.</param>
        /// <param name="addressDto">The DTO containing the new address information.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">User address updated successfully.</response>
        /// <response code="400">If the user ID is invalid or the address DTO is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the authenticated user is not authorized to update this address.</response>
        /// <response code="404">If the user with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{id:int}/address")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserAddress(int id, [FromBody] AddressDto addressDto)
        {
            var authUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API UpdateUserAddress START for TargetUserID: {TargetUserId}, AuthenticatedUserIDClaim: {AuthUserIdClaim}. AddressDTO: {@AddressDto}", 
                id, authUserIdClaim ?? "N/A", addressDto);

            if (id <= 0)
            {
                _logger.LogWarning("API UpdateUserAddress BAD_REQUEST: Invalid User ID: {TargetUserId}", id);
                return this.ApiBadRequest("Invalid User ID.");
            }
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateUserAddress BAD_REQUEST: Invalid model state for TargetUserID {TargetUserId}. Errors: {@ModelStateErrors}", 
                    id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("API UpdateUserAddress UNAUTHORIZED: Current user could not be identified. TargetUserID: {TargetUserId}", id);
                    return this.ApiUnauthorized("Could not identify current user.");
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                if (currentUser.Id != id && !isAdmin)
                {
                    _logger.LogWarning("API UpdateUserAddress FORBIDDEN: User {CurrentUserId} is not authorized to update address for UserID {TargetUserId}.", 
                        currentUser.Id, id);
                    return this.ApiForbidden("You are not authorized to update this address.");
                }

                await _userService.UpdateUserAddressAsync(id, addressDto);
                _logger.LogInformation("API UpdateUserAddress SUCCESS for TargetUserID: {TargetUserId}", id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API UpdateUserAddress NOT_FOUND: User with ID {TargetUserId} not found.", id);
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (UnauthorizedAccessException ex) 
            {
                 _logger.LogWarning(ex, "API UpdateUserAddress FORBIDDEN (service level) for TargetUserID: {TargetUserId}.", id);
                return this.ApiForbidden("Authorization error during address update.");
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogError(ex, "API UpdateUserAddress ERROR (Invalid Operation) for TargetUserID: {TargetUserId}. This might be an attempt to modify a key. AddressDTO: {@AddressDto}", id, addressDto);
                return this.ApiBadRequest("Invalid operation: " + ex.Message); 
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "API UpdateUserAddress ERROR for TargetUserID: {TargetUserId}. AddressDTO: {@AddressDto}", id, addressDto);
                return this.ApiInternalError("Error updating user address.", ex);
            }
        }

        /// <summary>
        /// Changes the password for the authenticated user.
        /// </summary>
        /// <param name="id">The ID of the user whose password is to be changed. Must match the authenticated user's ID.</param>
        /// <param name="passwordDto">The DTO containing the current and new passwords.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Password changed successfully.</response>
        /// <response code="400">If the user ID is invalid, DTO is invalid, or password change fails (e.g., current password incorrect, new password weak).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user attempts to change password for another user.</response>
        /// <response code="404">If the user is not found (should be rare if authenticated).</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/change-password")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordChangeDto passwordDto)
        {
            var authUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API ChangePassword START for TargetUserID: {TargetUserId}, AuthenticatedUserIDClaim: {AuthUserIdClaim}", 
                id, authUserIdClaim ?? "N/A");

            if (id <= 0)
            {
                _logger.LogWarning("API ChangePassword BAD_REQUEST: Invalid User ID: {TargetUserId}", id);
                return this.ApiBadRequest("Invalid User ID.");
            }
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API ChangePassword BAD_REQUEST: Invalid model state for TargetUserID {TargetUserId}. Errors: {@ModelStateErrors}", 
                    id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("API ChangePassword UNAUTHORIZED: Current user could not be identified. TargetUserID: {TargetUserId}", id);
                    return this.ApiUnauthorized("Could not identify current user.");
                }
                if (currentUser.Id != id)
                {
                    _logger.LogWarning("API ChangePassword FORBIDDEN: User {CurrentUserId} attempted to change password for UserID {TargetUserId}.", 
                        currentUser.Id, id);
                    return this.ApiForbidden("You can only change your own password.");
                }

                var success = await _userService.ChangeUserPasswordAsync(id, passwordDto.CurrentPassword, passwordDto.NewPassword);
                if (!success)
                {
                    _logger.LogWarning("API ChangePassword BAD_REQUEST: Password change failed for UserID {UserId}. Check current password and new password requirements.", id);
                    return this.ApiBadRequest("Password change failed. Please check your current password and ensure the new password meets all requirements.");
                }
                _logger.LogInformation("API ChangePassword SUCCESS for UserID: {UserId}", id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API ChangePassword NOT_FOUND: User with ID {TargetUserId} not found.", id);
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API ChangePassword ERROR for UserID: {TargetUserId}", id);
                return this.ApiInternalError("Error changing password.", ex);
            }
        }

        /// <summary>
        /// Retrieves a list of top users based on their EcoScore.
        /// This endpoint is publicly accessible.
        /// </summary>
        /// <param name="count">The number of top users to retrieve. Defaults to 10, max 50.</param>
        /// <returns>A list of users sorted by EcoScore in descending order.</returns>
        /// <response code="200">Successfully retrieved top eco users.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("top-eco")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTopEcoUsers([FromQuery] int count = 10)
        {
            _logger.LogInformation("API GetTopEcoUsers START. Count: {Count}", count);
            if (count <= 0 || count > 50)
            {
                _logger.LogInformation("API GetTopEcoUsers: Invalid count {InvalidCount} provided, defaulting to 10.", count);
                count = 10;
            }
            try
            {
                var users = await _userService.GetTopUsersByEcoScoreAsync(count);
                _logger.LogInformation("API GetTopEcoUsers SUCCESS. Retrieved: {RetrievedCount}", users?.Count() ?? 0);
                return this.ApiOk(users ?? new List<UserDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetTopEcoUsers ERROR");
                return this.ApiInternalError("Error retrieving top eco users.", ex);
            }
        }

        /// <summary>
        /// Retrieves badges for a specific user.
        /// This endpoint is publicly accessible.
        /// </summary>
        /// <param name="id">The ID of the user whose badges are to be retrieved.</param>
        /// <returns>A list of badges for the specified user.</returns>
        /// <response code="200">Successfully retrieved user badges.</response>
        /// <response code="400">If the user ID is invalid.</response>
        /// <response code="404">If the user with the specified ID is not found (if service throws KeyNotFoundException).</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}/badges")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BadgeDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserBadges(int id)
        {
            _logger.LogInformation("API GetUserBadges START for UserID: {UserId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("API GetUserBadges BAD_REQUEST: Invalid User ID: {UserId}", id);
                return this.ApiBadRequest("Invalid User ID.");
            }
            try
            {
                var badges = await _userService.GetUserBadgesAsync(id);
                _logger.LogInformation("API GetUserBadges SUCCESS for UserID: {UserId}. Badge count: {Count}", id, badges?.Count() ?? 0);
                return this.ApiOk(badges ?? new List<BadgeDto>());
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API GetUserBadges NOT_FOUND: User with ID {UserId} not found.", id);
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUserBadges ERROR for UserID: {UserId}", id);
                return this.ApiInternalError("Error retrieving user badges.", ex);
            }
        }

        /// <summary>
        /// Retrieves items listed by a specific user.
        /// This endpoint is publicly accessible.
        /// </summary>
        /// <param name="id">The ID of the user whose items are to be retrieved.</param>
        /// <returns>A list of items for the specified user.</returns>
        /// <response code="200">Successfully retrieved user items.</response>
        /// <response code="400">If the user ID is invalid.</response>
        /// <response code="404">If the user with the specified ID is not found (if service throws KeyNotFoundException).</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}/items")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserItems(int id)
        {
            _logger.LogInformation("API GetUserItems START for UserID: {UserId}", id);
            if (id <= 0)
            {
                _logger.LogWarning("API GetUserItems BAD_REQUEST: Invalid User ID: {UserId}", id);
                return this.ApiBadRequest("Invalid User ID.");
            }
            try
            {
                var items = await _userService.GetUserItemsAsync(id);
                _logger.LogInformation("API GetUserItems SUCCESS for UserID: {UserId}. Item count: {Count}", id, items?.Count() ?? 0);
                return this.ApiOk(items ?? new List<ItemDto>());
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API GetUserItems NOT_FOUND: User with ID {UserId} not found.", id);
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUserItems ERROR for UserID: {UserId}", id);
                return this.ApiInternalError("Error retrieving user items.", ex);
            }
        }

        /// <summary>
        /// Deactivates a user account.
        /// Requires authentication. The authenticated user can deactivate their own account, or an Admin can deactivate any user's account.
        /// </summary>
        /// <param name="id">The ID of the user to deactivate.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">User deactivated successfully.</response>
        /// <response code="400">If the user ID is invalid or deactivation fails (e.g., user already inactive).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the authenticated user is not authorized to deactivate this account.</response>
        /// <response code="404">If the user with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/deactivate")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var authUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API DeactivateUser START for TargetUserID: {TargetUserId}, AuthenticatedUserIDClaim: {AuthUserIdClaim}", 
                id, authUserIdClaim ?? "N/A");

            if (id <= 0)
            {
                _logger.LogWarning("API DeactivateUser BAD_REQUEST: Invalid User ID: {TargetUserId}", id);
                return this.ApiBadRequest("Invalid User ID.");
            }
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("API DeactivateUser UNAUTHORIZED: Current user could not be identified. TargetUserID: {TargetUserId}", id);
                    return this.ApiUnauthorized("Could not identify current user.");
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                if (currentUser.Id != id && !isAdmin)
                {
                    _logger.LogWarning("API DeactivateUser FORBIDDEN: User {CurrentUserId} is not authorized to deactivate UserID {TargetUserId}.", 
                        currentUser.Id, id);
                    return this.ApiForbidden("You are not authorized to deactivate this user.");
                }

                var success = await _userService.DeactivateUserAsync(id);
                if (!success)
                {
                    _logger.LogWarning("API DeactivateUser BAD_REQUEST: DeactivateUserAsync returned false for UserID {TargetUserId}. User might be already inactive or not found by service.", id);
                    return this.ApiBadRequest("Failed to deactivate user. User might already be inactive or another issue occurred.");
                }
                _logger.LogInformation("API DeactivateUser SUCCESS for TargetUserID: {TargetUserId}", id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API DeactivateUser NOT_FOUND: User with ID {TargetUserId} not found.", id);
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API DeactivateUser ERROR for TargetUserID: {TargetUserId}", id);
                return this.ApiInternalError("Error deactivating user.", ex);
            }
        }

        /// <summary>
        /// Uploads or updates the avatar for a specific user.
        /// Requires authentication. The authenticated user can update their own avatar, or an Admin can update any user's avatar.
        /// </summary>
        /// <param name="id">The ID of the user whose avatar is to be updated.</param>
        /// <param name="avatar">The avatar image file (JPG, PNG, WEBP; max 2MB).</param>
        /// <returns>An object containing the relative path to the uploaded avatar.</returns>
        /// <response code="200">Avatar uploaded successfully. Returns an object with the 'path' to the new avatar.</response>
        /// <response code="400">If user ID is invalid, no file is provided, or file is invalid (type/size).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the authenticated user is not authorized to change this avatar.</response>
        /// <response code="404">If the user with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs during upload or DB update.</response>
        [HttpPost("{id:int}/avatar")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [Consumes("multipart/form-data")] 
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)] 
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadAvatar(int id, IFormFile avatar) // Removed [FromForm]
        {
            var authUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API UploadAvatar START for TargetUserID: {TargetUserId}, AuthenticatedUserIDClaim: {AuthUserIdClaim}. File: {FileName}, Size: {FileSize}, ContentType: {ContentType}",
                id, authUserIdClaim ?? "N/A", avatar?.FileName, avatar?.Length, avatar?.ContentType);

            if (id <= 0)
            {
                _logger.LogWarning("API UploadAvatar BAD_REQUEST: Invalid User ID: {TargetUserId}", id);
                return this.ApiBadRequest("Invalid User ID.");
            }
            if (avatar == null || avatar.Length == 0)
            {
                _logger.LogWarning("API UploadAvatar BAD_REQUEST: No avatar file provided for UserID: {TargetUserId}", id);
                return this.ApiBadRequest("No avatar file provided. Ensure the form field name is 'avatar'.");
            }

            long maxFileSize = 2 * 1024 * 1024; // 2MB
            if (avatar.Length > maxFileSize)
            {
                _logger.LogWarning("API UploadAvatar BAD_REQUEST: File size {FileSize} exceeds limit of {MaxFileSize}MB for UserID: {TargetUserId}", 
                    avatar.Length, maxFileSize / (1024 * 1024) , id);
                return this.ApiBadRequest($"File size exceeds limit of {maxFileSize / 1024 / 1024} MB.");
            }
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(avatar.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("API UploadAvatar BAD_REQUEST: Invalid file type '{ContentType}' for UserID: {TargetUserId}. Allowed: {AllowedTypes}", 
                    avatar.ContentType, id, string.Join(", ", allowedTypes));
                return this.ApiBadRequest($"Invalid file type. Allowed: {string.Join(", ", allowedTypes)}.");
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("API UploadAvatar UNAUTHORIZED: Current user could not be identified. TargetUserID: {TargetUserId}", id);
                    return this.ApiUnauthorized("Could not identify current user.");
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                if (currentUser.Id != id && !isAdmin)
                {
                    _logger.LogWarning("API UploadAvatar FORBIDDEN: User {CurrentUserId} is not authorized to change avatar for UserID {TargetUserId}.", 
                        currentUser.Id, id);
                    return this.ApiForbidden("You are not authorized to change this avatar.");
                }

                var userToUpdate = await _userManager.FindByIdAsync(id.ToString());
                if (userToUpdate == null)
                {
                    _logger.LogWarning("API UploadAvatar NOT_FOUND: User with ID {TargetUserId} not found.", id);
                    return this.ApiNotFound($"User with ID {id} not found.");
                }

                var oldAvatarRelativePath = userToUpdate.AvatarPath;
                string newRelativePath;
                using (var stream = avatar.OpenReadStream())
                {
                    newRelativePath = await _photoService.UploadPhotoAsync(stream, avatar.FileName, avatar.ContentType, "avatars");
                }
                _logger.LogInformation("Avatar file uploaded to path: {NewPath} for UserID: {TargetUserId}", newRelativePath, id);

                userToUpdate.AvatarPath = newRelativePath;
                var updateResult = await _userManager.UpdateAsync(userToUpdate);

                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update user avatar path in DB for UserID: {TargetUserId}. Errors: {Errors}. Attempting to delete uploaded file: {NewPath}", 
                        id, string.Join(", ", updateResult.Errors.Select(e => e.Description)), newRelativePath);
                    await _photoService.DeletePhotoByPathAsync(newRelativePath); 
                    return this.ApiInternalError("Failed to save avatar information to user profile.");
                }
                _logger.LogInformation("Successfully updated avatar path in DB for UserID: {TargetUserId}", id);

                if (!string.IsNullOrEmpty(oldAvatarRelativePath) && oldAvatarRelativePath != newRelativePath)
                {
                    _logger.LogInformation("Attempting to delete old avatar file: {OldPath} for UserID: {TargetUserId}", oldAvatarRelativePath, id);
                    var deleted = await _photoService.DeletePhotoByPathAsync(oldAvatarRelativePath);
                    _logger.LogInformation("Deletion of old avatar file {OldPath} for UserID {TargetUserId} result: {Deleted}", oldAvatarRelativePath, id, deleted);
                }
                
                return this.ApiOk(new { path = newRelativePath }, "Avatar updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UploadAvatar ERROR for UserID: {TargetUserId}", id);
                return this.ApiInternalError("An unexpected error occurred while uploading the avatar.", ex);
            }
        }
    }
}
