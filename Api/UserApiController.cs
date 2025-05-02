using System;
using System.Collections.Generic;
using System.Security.Claims;    // Dla User.FindFirstValue
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Models.API;      // Dla ApiResponse<T> i ApiResponse
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs.Auth; // Potrzebne dla PasswordChangeDto
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;      // Dla StatusCodes
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;             // <<<=== USING dla ApiControllerExtensions

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Rozważ autoryzację na poziomie kontrolera lub poszczególnych akcji
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        // Usunięto SignInManager - nie jest potrzebny bez logowania
        // Usunięto IJwtTokenService - nie jest potrzebny bez logowania
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            UserManager<User> userManager,
            // Usunięto SignInManager
            // Usunięto IJwtTokenService
            ILogger<UsersController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/users/{id:int}
        [HttpGet("{id:int}")]
        [Authorize] // Wymaga autoryzacji
        public async Task<IActionResult> GetUser(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                if (currentUser.Id != id && !isAdmin)
                {
                    return this.ApiForbidden("You are not authorized to view this user's details.");
                }

                var user = await _userService.GetUserWithDetailsAsync(id); // Zakładam, że zwraca UserWithDetailsDto

                if (user == null)
                {
                    return this.ApiNotFound($"User with ID {id} not found.");
                }

                return this.ApiOk(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user data for ID: {UserId}", id);
                return this.ApiInternalError("Error retrieving user data", ex);
            }
        }

        // GET: api/users/current
        [HttpGet("current")]
        [Authorize] // Wymaga autoryzacji
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return this.ApiUnauthorized("Current user could not be identified.");
                }

                var userDto = await _userService.GetUserByIdAsync(currentUser.Id); // Zakładam, że zwraca UserDto
                if (userDto == null)
                {
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

        // === USUNIĘTO METODĘ Register ===

        // === USUNIĘTO METODĘ Login ===

        // PUT: api/users/{id:int}
        [HttpPut("{id:int}")]
        [Authorize] // Wymaga autoryzacji
        public async Task<IActionResult> UpdateUserProfile(int id, [FromBody] UserUpdateDto userDto) // Dodano FromBody
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
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found");
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
        [Authorize] // Wymaga autoryzacji
        public async Task<IActionResult> UpdateUserAddress(int id, [FromBody] AddressDto addressDto) // Dodano FromBody
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
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found");
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
        [Authorize] // Wymaga autoryzacji
        public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordChangeDto passwordDto) // Dodano FromBody
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
                    return this.ApiBadRequest("Current password might be incorrect or the new password does not meet requirements.");
                }

                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user password. UserId: {UserId}", id);
                return this.ApiInternalError("Error changing password", ex);
            }
        }

        // GET: api/users/top-eco
        [HttpGet("top-eco")]
        [AllowAnonymous] // Prawdopodobnie publiczny endpoint
        public async Task<IActionResult> GetTopEcoUsers([FromQuery] int count = 10)
        {
            if (count <= 0 || count > 50) count = 10; // Walidacja + limit
            try
            {
                var users = await _userService.GetTopUsersByEcoScoreAsync(count); // Zakładam, że zwraca IEnumerable<UserDto>
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
        [AllowAnonymous] // Prawdopodobnie publiczny endpoint
        public async Task<IActionResult> GetUserBadges(int id)
        {
             if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
            try
            {
                // Można dodać sprawdzenie, czy użytkownik istnieje, jeśli GetUserBadgesAsync tego nie robi
                var badges = await _userService.GetUserBadgesAsync(id); // Zakładam, że zwraca IEnumerable<BadgeDto>
                return this.ApiOk(badges);
            }
            catch (KeyNotFoundException) // Jeśli serwis rzuci, gdy user nie istnieje
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
        [AllowAnonymous] // Prawdopodobnie publiczny endpoint
        public async Task<IActionResult> GetUserItems(int id)
        {
             if (id <= 0) return this.ApiBadRequest("Invalid User ID.");
            try
            {
                 // Można dodać sprawdzenie, czy użytkownik istnieje
                var items = await _userService.GetUserItemsAsync(id); // Zakładam, że zwraca IEnumerable<ItemDto>
                return this.ApiOk(items);
            }
            catch (KeyNotFoundException) // Jeśli serwis rzuci
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
        [Authorize] // Wymaga autoryzacji
        public async Task<IActionResult> DeactivateUser(int id)
        {
            if (id <= 0) return this.ApiBadRequest("Invalid User ID.");

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                 if (currentUser == null) return this.ApiUnauthorized("Could not identify current user.");
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                // Tylko admin lub sam użytkownik może dezaktywować
                if (currentUser.Id != id && !isAdmin)
                {
                    return this.ApiForbidden("You are not authorized to deactivate this user.");
                }

                // DeactivateUserAsync zwraca bool
                var success = await _userService.DeactivateUserAsync(id);

                if (!success)
                {
                    // Może już być nieaktywny lub inny problem
                    return this.ApiBadRequest("Failed to deactivate user. User might already be inactive or another issue occurred.");
                }

                // Pomyślna dezaktywacja - często używa się NoContent
                return this.ApiNoContent();
                // Lub jeśli chcesz potwierdzenie: return this.ApiOk("User deactivated successfully.");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound($"User with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user. UserId: {UserId}", id);
                return this.ApiInternalError("Error deactivating user", ex);
            }
        }
    }

    // Definicja PasswordChangeDto powinna być w pliku DTO
    /*
    using System.ComponentModel.DataAnnotations;
    namespace LeafLoop.Services.DTOs.Auth // Lub LeafLoop.Services.DTOs.Users
    {
         public class PasswordChangeDto
         {
             [Required]
             public string CurrentPassword { get; set; }

             [Required]
             [MinLength(8)] // Przykładowe wymaganie
             public string NewPassword { get; set; }

             [Required]
             [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
             public string ConfirmNewPassword { get; set; }
         }
    }
    */
}