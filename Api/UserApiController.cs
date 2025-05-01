using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Services.DTOs.Auth; 
namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserWithDetailsDto>> GetUser(int id)
        {
            try
            {
                // Check if current user is requesting their own data or is an admin
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                if (currentUser.Id != id && !isAdmin)
                {
                    return Forbid();
                }

                var user = await _userService.GetUserWithDetailsAsync(id);
                
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user data for ID: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving user data");
            }
        }

        // GET: api/users/current
        [HttpGet("current")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                if (currentUser == null)
                {
                    return NotFound();
                }

                var userDto = await _userService.GetUserByIdAsync(currentUser.Id);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user data");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving user data");
            }
        }

        // POST: api/users/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Register(UserRegistrationDto registrationDto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userService.GetUserByEmailAsync(registrationDto.Email);
                if (existingUser != null)
                {
                    return Conflict("User with this email already exists");
                }

                // Validate passwords match
                if (registrationDto.Password != registrationDto.ConfirmPassword)
                {
                    return BadRequest("Passwords do not match");
                }

                // Register user
                var userId = await _userService.RegisterUserAsync(registrationDto);
                
                // Return the newly created user
                var userDto = await _userService.GetUserByIdAsync(userId);
                
                return CreatedAtAction(nameof(GetUser), new { id = userId }, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error registering user");
            }
        }

        // POST: api/users/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                
                if (user == null)
                {
                    return Unauthorized("Invalid email or password");
                }

                // Check password
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                
                if (!result.Succeeded)
                {
                    return Unauthorized("Invalid email or password");
                }

                // Generate JWT token or other authentication token here
                // For simplicity, we'll return a placeholder
                // In a real application, you'd implement JWT token generation
                
                return Ok(new { Token = "JWT_TOKEN_WOULD_BE_HERE", UserId = user.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error during login");
            }
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile(int id, UserUpdateDto userDto)
        {
            if (id != userDto.Id)
            {
                return BadRequest("User ID mismatch");
            }
            
            try
            {
                // Check if current user is updating their own profile or is an admin
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                if (currentUser.Id != id && !isAdmin)
                {
                    return Forbid();
                }

                await _userService.UpdateUserProfileAsync(userDto);
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"User with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile. UserId: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating user profile");
            }
        }

        // PUT: api/users/5/address
        [HttpPut("{id}/address")]
        [Authorize]
        public async Task<IActionResult> UpdateUserAddress(int id, AddressDto addressDto)
        {
            try
            {
                // Check if current user is updating their own address or is an admin
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                if (currentUser.Id != id && !isAdmin)
                {
                    return Forbid();
                }

                await _userService.UpdateUserAddressAsync(id, addressDto);
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"User with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user address. UserId: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating user address");
            }
        }

        // POST: api/users/5/change-password
        [HttpPost("{id}/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordChangeDto passwordDto)
        {
            try
            {
                // Check if current user is changing their own password
                var currentUser = await _userManager.GetUserAsync(User);
                
                if (currentUser.Id != id)
                {
                    return Forbid("You can only change your own password");
                }

                var success = await _userService.ChangeUserPasswordAsync(id, passwordDto.CurrentPassword, passwordDto.NewPassword);
                
                if (!success)
                {
                    return BadRequest("Current password is incorrect");
                }
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"User with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user password. UserId: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error changing password");
            }
        }

        // GET: api/users/top-eco
        [HttpGet("top-eco")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetTopEcoUsers([FromQuery] int count = 10)
        {
            try
            {
                var users = await _userService.GetTopUsersByEcoScoreAsync(count);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top eco users");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving users");
            }
        }

        // GET: api/users/5/badges
        [HttpGet("{id}/badges")]
        public async Task<ActionResult<IEnumerable<BadgeDto>>> GetUserBadges(int id)
        {
            try
            {
                var badges = await _userService.GetUserBadgesAsync(id);
                return Ok(badges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user badges. UserId: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving badges");
            }
        }

        // GET: api/users/5/items
        [HttpGet("{id}/items")]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetUserItems(int id)
        {
            try
            {
                var items = await _userService.GetUserItemsAsync(id);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user items. UserId: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving items");
            }
        }

        // POST: api/users/5/deactivate
        [HttpPost("{id}/deactivate")]
        [Authorize]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                // Check if current user is deactivating their own account or is an admin
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                if (currentUser.Id != id && !isAdmin)
                {
                    return Forbid();
                }

                var success = await _userService.DeactivateUserAsync(id);
                
                if (!success)
                {
                    return BadRequest("Failed to deactivate user");
                }
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"User with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user. UserId: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deactivating user");
            }
        }
    }

    // Helper class for login


    // Helper class for password change
    public class PasswordChangeDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}