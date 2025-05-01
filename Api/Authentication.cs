using System;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs.Auth;
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
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserService _userService;
        private readonly IUserSessionService _sessionService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IJwtTokenService jwtTokenService,
            IUserService userService,
            IUserSessionService sessionService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
            _sessionService = sessionService;
            _logger = logger;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register([FromBody] UserRegistrationDto registrationDto)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(registrationDto.Email);
                if (existingUser != null)
                {
                    return Conflict(new { message = "Email is already registered" });
                }

                // Validate password match
                if (registrationDto.Password != registrationDto.ConfirmPassword)
                {
                    return BadRequest(new { message = "Passwords do not match" });
                }

                // Create the user
                var userId = await _userService.RegisterUserAsync(registrationDto);
                
                // Get the user for token generation
                var user = await _userManager.FindByIdAsync(userId.ToString());
                
                // Generate token
                var token = await _jwtTokenService.GenerateTokenAsync(user);
                
                // Create user session
                await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                
                var userDto = await _userService.GetUserByIdAsync(userId);
                
                // Return the token and user info
                return Ok(new TokenResponseDto
                {
                    Token = token,
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during registration" });
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                // Find the user by email
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Check if the user is active
                if (!user.IsActive)
                {
                    return Unauthorized(new { message = "Account is deactivated" });
                }

                // Verify password
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Update last activity time
                user.LastActivity = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate token
                var token = await _jwtTokenService.GenerateTokenAsync(user);
                
                // Create user session
                await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                
                var userDto = await _userService.GetUserByIdAsync(user.Id);
                
                // Return the token and user info
                return Ok(new TokenResponseDto
                {
                    Token = token,
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during login" });
            }
        }
    }

    // Helper class for login
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}