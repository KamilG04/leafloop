using LeafLoop.Models;
using LeafLoop.Models.API;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs.Auth;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace LeafLoop.Api;

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
        _logger.LogInformation("API GetUser START for ID: {RequestedId}. Auth User: {AuthUser}", id,
            User.Identity?.Name ?? "N/A");
        if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (currentUser.Id != id && !isAdmin)
                return this.ApiForbidden("You are not authorized to view this user's details.");
            var userDto = await _userService.GetUserWithDetailsAsync(id);
            if (userDto == null) return this.ApiNotFound($"User with ID {id} not found.");
            return this.ApiOk(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API GetUser ERROR for ID: {UserId}", id);
            return this.ApiInternalError("Error retrieving user data", ex);
        }
    }

    [HttpGet("current")]
    [Authorize(Policy = "ApiAuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser()
    {
        _logger.LogInformation("API GetCurrentUser START. Auth User: {AuthUser}", User.Identity?.Name ?? "N/A");
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return this.ApiUnauthorized("Current user could not be identified.");
            var userDto = await _userService.GetUserByIdAsync(currentUser.Id);
            if (userDto == null)
            {
                _logger.LogWarning("Could not find user details for ID {UserId} after GetUserAsync succeeded.",
                    currentUser.Id);
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

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ApiAuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateUserProfile(int id, [FromBody] UserUpdateDto userDto)
    {
        _logger.LogInformation("API UpdateUserProfile START for ID: {UserId}. Auth User: {AuthUser}", id,
            User.Identity?.Name ?? "N/A");
        if (id != userDto.Id) return this.ApiBadRequest("User ID mismatch in URL and body.");
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (currentUser.Id != id && !isAdmin)
                return this.ApiForbidden("You are not authorized to update this profile.");
            await _userService.UpdateUserProfileAsync(userDto);
            return this.ApiNoContent();
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

    [HttpPut("{id:int}/address")]
    [Authorize(Policy = "ApiAuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateUserAddress(int id, [FromBody] AddressDto addressDto)
    {
        _logger.LogInformation("API UpdateUserAddress START for ID: {UserId}. Auth User: {AuthUser}", id,
            User.Identity?.Name ?? "N/A");
        if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (currentUser.Id != id && !isAdmin)
                return this.ApiForbidden("You are not authorized to update this address.");
            await _userService.UpdateUserAddressAsync(id, addressDto);
            return this.ApiNoContent();
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

    [HttpPost("{id:int}/change-password")]
    [Authorize(Policy = "ApiAuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordChangeDto passwordDto)
    {
        _logger.LogInformation("API ChangePassword START for ID: {UserId}. Auth User: {AuthUser}", id,
            User.Identity?.Name ?? "N/A");
        if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
            if (currentUser.Id != id) return this.ApiForbidden("You can only change your own password.");
            var success =
                await _userService.ChangeUserPasswordAsync(id, passwordDto.CurrentPassword, passwordDto.NewPassword);
            if (!success)
                return this.ApiBadRequest(
                    "Password change failed. Check current password and new password requirements.");
            return this.ApiNoContent();
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

    [HttpGet("top-eco")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopEcoUsers([FromQuery] int count = 10)
    {
        _logger.LogInformation("API GetTopEcoUsers START. Count: {Count}", count);
        if (count <= 0 || count > 50) count = 10;
        try
        {
            var users = await _userService.GetTopUsersByEcoScoreAsync(count);
            return this.ApiOk(users ?? new List<UserDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top eco users");
            return this.ApiInternalError("Error retrieving users", ex);
        }
    }

    [HttpGet("{id:int}/badges")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BadgeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserBadges(int id)
    {
        _logger.LogInformation("API GetUserBadges START for ID: {UserId}", id);
        if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
        try
        {
            var badges = await _userService.GetUserBadgesAsync(id);
            return this.ApiOk(badges ?? new List<BadgeDto>());
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

    [HttpGet("{id:int}/items")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserItems(int id)
    {
        _logger.LogInformation("API GetUserItems START for ID: {UserId}", id);
        if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
        try
        {
            var items = await _userService.GetUserItemsAsync(id);
            return this.ApiOk(items ?? new List<ItemDto>());
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

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = "ApiAuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        _logger.LogInformation("API DeactivateUser START for ID: {UserId}. Auth User: {AuthUser}", id,
            User.Identity?.Name ?? "N/A");
        if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (currentUser.Id != id && !isAdmin)
                return this.ApiForbidden("You are not authorized to deactivate this user.");
            var success = await _userService.DeactivateUserAsync(id);
            if (!success)
                return this.ApiBadRequest(
                    "Failed to deactivate user. User might already be inactive or another issue occurred.");
            return this.ApiNoContent();
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

    [HttpPost("{id:int}/avatar")]
    [Authorize(Policy = "ApiAuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar(int id, [FromForm] IFormFile avatar)
    {
        _logger.LogInformation(
            "API UploadAvatar START for UserID: {UserId}. File received: {FileName}, Size: {FileSize}, ContentType: {ContentType}",
            id, avatar?.FileName ?? "N/A", avatar?.Length ?? 0, avatar?.ContentType ?? "N/A");

        if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
        if (avatar == null || avatar.Length == 0)
            return this.ApiBadRequest("No avatar file provided. Ensure the form field name is 'avatar'.");

        long maxFileSize = 2 * 1024 * 1024;
        if (avatar.Length > maxFileSize)
            return this.ApiBadRequest($"File size exceeds limit of {maxFileSize / 1024 / 1024} MB.");
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(avatar.ContentType.ToLowerInvariant()))
            return this.ApiBadRequest("Invalid file type. Allowed: JPG, PNG, WEBP.");

        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (currentUser.Id != id && !isAdmin)
            {
                _logger.LogWarning("User {CurrentUserId} forbidden to upload avatar for User {TargetUserId}",
                    currentUser.Id, id);
                return this.ApiForbidden("You are not authorized to change this avatar.");
            }

            var userToUpdate = await _userManager.FindByIdAsync(id.ToString());
            if (userToUpdate == null) return this.ApiNotFound($"User with ID {id} not found.");

            var oldAvatarRelativePath = userToUpdate.AvatarPath;

            string newRelativePath;
            using (var stream = avatar.OpenReadStream())
            {
                newRelativePath =
                    await _photoService.UploadPhotoAsync(stream, avatar.FileName, avatar.ContentType, "avatars");
            }

            _logger.LogInformation("Avatar file uploaded for UserID: {UserId}. New relative path: {Path}", id,
                newRelativePath);

            userToUpdate.AvatarPath = newRelativePath;
            var updateResult = await _userManager.UpdateAsync(userToUpdate);

            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to update user avatar path in DB for UserID: {UserId}. Errors: {Errors}", id,
                    string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                await _photoService.DeletePhotoByPathAsync(newRelativePath);
                return this.ApiInternalError("Failed to save avatar information to user profile.");
            }

            _logger.LogInformation("Successfully updated avatar path in DB for UserID: {UserId}", id);

            if (!string.IsNullOrEmpty(oldAvatarRelativePath))
            {
                _logger.LogInformation("Attempting to delete old avatar file: {OldPath}", oldAvatarRelativePath);
                var deleted = await _photoService.DeletePhotoByPathAsync(oldAvatarRelativePath);
                _logger.LogInformation("Deletion of old avatar file {OldPath} result: {Deleted}", oldAvatarRelativePath,
                    deleted);
            }

            return this.ApiOk(newRelativePath, "Avatar updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for UserID: {UserId}", id);
            return this.ApiInternalError("An unexpected error occurred while uploading the avatar.", ex);
        }
    }
}