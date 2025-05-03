using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Models.API;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs.Auth;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api; // Dla ApiControllerExtensions

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")] // Jawnie określ typ zwracany przez kontroler
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            UserManager<User> userManager,
            ILogger<UsersController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/users/{id:int}
        /// <summary>
        /// Gets detailed information for a specific user.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>User details.</returns>
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
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                // Allow access if user is requesting their own profile or if the requester is an Admin
                if (currentUser.Id != id && !isAdmin)
                {
                    return this.ApiForbidden("You are not authorized to view this user's details.");
                }

                var userDto = await _userService.GetUserWithDetailsAsync(id);
                if (userDto == null)
                {
                    return this.ApiNotFound($"User with ID {id} not found.");
                }

                return this.ApiOk(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user data for ID: {UserId}", id);
                return this.ApiInternalError("Error retrieving user data", ex);
            }
        }

        // GET: api/users/current
        [HttpGet("current")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return this.ApiUnauthorized("Current user could not be identified.");
                }

                var userDto = await _userService.GetUserByIdAsync(currentUser.Id);
                if (userDto == null)
                {
                    _logger.LogWarning("Could not find user details for ID {UserId} after GetUserAsync succeeded.", currentUser.Id);
                    return this.ApiNotFound("Current user details could not be found.");
                }
                return this.ApiOk(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user data");
                return this.ApiInternalError("Error retrieving current user data", ex);
            }
        }

        // PUT: api/users/{id:int}
        [HttpPut("{id:int}")]
        [Authorize(Policy = "ApiAuthPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserProfile(int id, [FromBody] UserUpdateDto userDto)
        {
            if (id != userDto.Id)
            {
                return this.ApiBadRequest("User ID mismatch in URL and body.");
            }
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                if (currentUser.Id != id && !isAdmin)
                {
                    return this.ApiForbidden("You are not authorized to update this profile.");
                }

                await _userService.UpdateUserProfileAsync(userDto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (UnauthorizedAccessException)
            {
                return this.ApiForbidden("Authorization error during update.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile. UserId: {UserId}", id);
                return this.ApiInternalError("Error updating user profile", ex);
            }
        }

        // PUT: api/users/{id:int}/address
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
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                if (currentUser.Id != id && !isAdmin)
                {
                    return this.ApiForbidden("You are not authorized to update this address.");
                }

                await _userService.UpdateUserAddressAsync(id, addressDto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (UnauthorizedAccessException)
            {
                return this.ApiForbidden("Authorization error during address update.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user address. UserId: {UserId}", id);
                return this.ApiInternalError("Error updating user address", ex);
            }
        }

        // POST: api/users/{id:int}/change-password
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
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
                if (currentUser.Id != id)
                {
                    return this.ApiForbidden("You can only change your own password.");
                }

                var success = await _userService.ChangeUserPasswordAsync(id, passwordDto.CurrentPassword, passwordDto.NewPassword);
                if (!success)
                {
                    return this.ApiBadRequest("Password change failed. Please check current password and ensure the new password meets complexity requirements.");
                }

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user password. UserId: {UserId}", id);
                return this.ApiInternalError("Error changing password", ex);
            }
        }

        // GET: api/users/top-eco
        [HttpGet("top-eco")]  
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTopEcoUsers([FromQuery] int count = 10)
        {
            if (count <= 0 || count > 50) count = 10;
            try
            {
                var users = await _userService.GetTopUsersByEcoScoreAsync(count);
                return this.ApiOk(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top eco users");
                return this.ApiInternalError("Error retrieving users", ex);
            }
        }

        // GET: api/users/{id:int}/badges
        [HttpGet("{id:int}/badges")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BadgeDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserBadges(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
            try
            {
                var badges = await _userService.GetUserBadgesAsync(id);
                return this.ApiOk(badges);
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user badges. UserId: {UserId}", id);
                return this.ApiInternalError("Error retrieving badges", ex);
            }
        }

        // GET: api/users/{id:int}/items
        [HttpGet("{id:int}/items")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserItems(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
            try
            {
                var items = await _userService.GetUserItemsAsync(id);
                return this.ApiOk(items);
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user items. UserId: {UserId}", id);
                return this.ApiInternalError("Error retrieving items", ex);
            }
        }

        // POST: api/users/{id:int}/deactivate
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
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                if (currentUser.Id != id && !isAdmin)
                {
                    return this.ApiForbidden("You are not authorized to deactivate this user.");
                }

                var success = await _userService.DeactivateUserAsync(id);
                if (!success)
                {
                    return this.ApiBadRequest("Failed to deactivate user. User might already be inactive or another issue occurred.");
                }

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user. UserId: {UserId}", id);
                return this.ApiInternalError("Error deactivating user", ex);
            }
        }
    }
}
