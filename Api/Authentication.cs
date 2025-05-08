using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs.Auth;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
                _logger.LogError(ex, "Error occurred during user registration for email {Email}", registrationDto.Email);
                 // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                return this.ApiInternalError("An error occurred during registration. Please try again later.", ex);
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
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
                _logger.LogError(ex, "Error occurred during user login for email {Email}", loginDto.Email);
                 // Metody rozszerzeń zwracają IActionResult, co jest zgodne z typem metody
                 return this.ApiInternalError("An error occurred during login. Please try again later.", ex);
            }
        }
        [HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
{
    if (forgotPasswordDto == null || string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
    {
        return this.ApiBadRequest("Email is required.");
    }

    try
    {
        var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
        
        // Don't reveal if the user exists or not for security reasons
        if (user == null || !user.IsActive)
        {
            return this.ApiOk("If your email is registered, you will receive password reset instructions.");
        }

        // Generate reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // TODO: Send email with the token (ideally through an email service)
        // For now, we'll return the token in the response (not recommended for production)
        _logger.LogInformation("Password reset token generated for user {Email}: {Token}", forgotPasswordDto.Email, token);
        
        return this.ApiOk("Password reset instructions have been sent to your email.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred during password reset request for email {Email}", forgotPasswordDto.Email);
        return this.ApiInternalError("An error occurred. Please try again later.", ex);
    }
}

[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
{
    if (resetPasswordDto == null || 
        string.IsNullOrWhiteSpace(resetPasswordDto.Email) || 
        string.IsNullOrWhiteSpace(resetPasswordDto.Token) || 
        string.IsNullOrWhiteSpace(resetPasswordDto.NewPassword))
    {
        return this.ApiBadRequest("All fields are required.");
    }

    try
    {
        var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
        if (user == null)
        {
            // Don't reveal the user doesn't exist
            return this.ApiBadRequest("Password reset failed.");
        }

        var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Password reset failed for user {Email}. Errors: {Errors}", 
                resetPasswordDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return this.ApiBadRequest("Password reset failed. The link may have expired.");
        }

        // Update last activity
        user.LastActivity = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return this.ApiOk("Your password has been reset successfully. You can now log in with your new password.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred during password reset for email {Email}", resetPasswordDto.Email);
        return this.ApiInternalError("An error occurred. Please try again later.", ex);
    }
}
    }
    
}