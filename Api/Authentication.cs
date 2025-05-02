using System;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Models.API;      // Dla ApiResponse<T> i ApiResponse
using LeafLoop.Services;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs.Auth;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;             // Dla ApiControllerExtensions

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
        // === ZMIANA SYGNATURY z powrotem na Task<IActionResult> ===
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
        {
            if (registrationDto == null || string.IsNullOrWhiteSpace(registrationDto.Email) || string.IsNullOrWhiteSpace(registrationDto.Password))
            {
                // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                return this.ApiBadRequest("Registration data is incomplete.");
            }

            try
            {
                var existingUser = await _userManager.FindByEmailAsync(registrationDto.Email);
                if (existingUser != null)
                {
                    return this.ApiError(StatusCodes.Status409Conflict, "Email is already registered.");
                }

                if (registrationDto.Password != registrationDto.ConfirmPassword)
                {
                    return this.ApiBadRequest("Passwords do not match.");
                }

                var userId = await _userService.RegisterUserAsync(registrationDto);

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                     _logger.LogError("User not found immediately after registration. UserID: {UserId}", userId);
                     return this.ApiInternalError("Failed to retrieve user after registration.");
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                var userDto = await _userService.GetUserByIdAsync(userId);

                var tokenResponse = new TokenResponseDto
                {
                    Token = token,
                    User = userDto
                };

                // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                return this.ApiOk(tokenResponse, "User registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for email {Email}", registrationDto?.Email);
                 // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                return this.ApiInternalError("An error occurred during registration. Please try again later.", ex);
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        // === ZMIANA SYGNATURY z powrotem na Task<IActionResult> ===
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
             if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                 // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                return this.ApiBadRequest("Login data is incomplete.");
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                    return this.ApiUnauthorized("Invalid email or password.");
                }

                if (!user.IsActive)
                {
                     // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                    return this.ApiUnauthorized("Account is deactivated. Please contact support.");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed login attempt for email {Email}", loginDto.Email);
                    // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                    return this.ApiUnauthorized("Invalid email or password.");
                }

                user.LastActivity = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                     _logger.LogError("Failed to update LastActivity for user {UserId}. Errors: {@Errors}", user.Id, updateResult.Errors);
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                var userDto = await _userService.GetUserByIdAsync(user.Id);

                var tokenResponse = new TokenResponseDto
                {
                    Token = token,
                    User = userDto
                };

                // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                return this.ApiOk(tokenResponse, "Login successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login for email {Email}", loginDto?.Email);
                 // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                 return this.ApiInternalError("An error occurred during login. Please try again later.", ex);
            }
        }
    }
    // Pozostałe definicje DTO bez zmian...
}