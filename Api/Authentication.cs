using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse and ApiResponse<T>
using LeafLoop.Services.DTOs.Auth; // For DTOs like UserRegistrationDto, LoginDto etc.
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // For CookieOptions, StatusCodes
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq; // For LINQ operations if any (e.g. on IdentityError)
using System.Threading.Tasks;
using LeafLoop.Services.DTOs; // For Task

namespace LeafLoop.Api
{
    /// <summary>
    /// Handles authentication processes such as user registration, login, and password management.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")] // Indicates all actions in this controller produce JSON
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
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves the JWT for an authenticated user (typically for SPAs after cookie-based auth).
        /// </summary>
        /// <remarks>
        /// This endpoint is intended for Single Page Applications that might need direct access to the JWT
        /// after the user has been authenticated via an HttpOnly cookie mechanism.
        /// The user must already be authenticated via the 'ApiAuthPolicy'.
        /// </remarks>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the JWT if found, or a 404 Not Found response.
        /// </returns>
        /// <response code="200">Returns the JWT.</response>
        /// <response code="401">If the user is not authenticated (handled by Authorize attribute).</response>
        /// <response code="404">If the 'auth_token' cookie is not found despite authorization.</response>
        [HttpGet("token")]
        [Authorize(Policy = "ApiAuthPolicy")] // Requires user to be authenticated via JWT (from cookie)
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)] // Assuming simple object like { token: "value" }
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public IActionResult GetTokenForSpa()
        {
            _logger.LogInformation("API GetTokenForSpa START for authenticated user: {UserName}", User.Identity?.Name ?? "N/A");
            var tokenString = HttpContext.Request.Cookies["auth_token"];

            if (string.IsNullOrEmpty(tokenString))
            {
                // This case should ideally not be reached if [Authorize] worked based on the cookie.
                _logger.LogWarning("User {UserName} is authorized, but 'auth_token' cookie was not found in GetTokenForSpa.", User.Identity?.Name);
                return this.ApiNotFound("Token not found in cookie, though user is authorized.");
            }
            _logger.LogInformation("API GetTokenForSpa SUCCESS for user: {UserName}", User.Identity?.Name);
            // Return the token in a simple object.
            // The ApiOk extension will wrap this in your standard ApiResponse.
            return this.ApiOk(new { token = tokenString });
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="registrationDto">The user registration data.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the JWT and user details upon successful registration,
        /// or an error response.
        /// </returns>
        /// <response code="200">User registered successfully. Returns JWT and user details.</response>
        /// <response code="400">If registration data is invalid or passwords do not match.</response>
        /// <response code="409">If the email is already registered.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
        {
            _logger.LogInformation("API Register attempt for email: {Email}", registrationDto?.Email ?? "N/A");

            if (registrationDto == null || string.IsNullOrWhiteSpace(registrationDto.Email) ||
                string.IsNullOrWhiteSpace(registrationDto.Password) || string.IsNullOrWhiteSpace(registrationDto.ConfirmPassword))
            {
                return this.ApiBadRequest("Registration data is incomplete. Email, password, and confirm password are required.");
            }

            if (registrationDto.Password != registrationDto.ConfirmPassword)
            {
                return this.ApiBadRequest("Passwords do not match.");
            }

            try
            {
                var existingUser = await _userManager.FindByEmailAsync(registrationDto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already registered.", registrationDto.Email);
                    return this.ApiError(StatusCodes.Status409Conflict, "Email is already registered.");
                }

                var userId = await _userService.RegisterUserAsync(registrationDto); // This can throw ApplicationException
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null) // Should not happen if RegisterUserAsync succeeded and returned a valid ID
                {
                    _logger.LogError("User not found immediately after registration. UserID: {UserId}", userId);
                    return this.ApiInternalError("Failed to retrieve user after registration.");
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);

                // Set HttpOnly cookie for web clients
                Response.Cookies.Append(
                    "auth_token",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(1), // TODO: Consider making cookie lifetime configurable
                        Path = "/"
                    });

                await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                var userDto = await _userService.GetUserByIdAsync(userId); // Get DTO for the response

                var tokenResponse = new TokenResponseDto
                {
                    Token = token, // Token is also returned in body for non-browser clients
                    User = userDto
                };

                _logger.LogInformation("API Register SUCCESS for email: {Email}, UserID: {UserId}", registrationDto.Email, userId);
                return this.ApiOk(tokenResponse, "User registered successfully.");
            }
            catch (ApplicationException appEx) // Catch specific exception from RegisterUserAsync
            {
                 _logger.LogWarning(appEx, "Registration failed for email {Email} due to application exception.", registrationDto.Email);
                 return this.ApiBadRequest(appEx.Message); // Use message from ApplicationException
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user registration for email {Email}", registrationDto.Email);
                return this.ApiInternalError("An unexpected error occurred during registration.", ex);
            }
        }

        /// <summary>
        /// Logs in an existing user.
        /// </summary>
        /// <param name="loginDto">The user login data.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the JWT and user details upon successful login,
        /// or an error response.
        /// </returns>
        /// <response code="200">Login successful. Returns JWT and user details.</response>
        /// <response code="400">If login data is incomplete.</response>
        /// <response code="401">If login attempt is invalid (wrong credentials, inactive account).</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("API Login attempt for email: {Email}", loginDto?.Email ?? "N/A");
            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return this.ApiBadRequest("Login data is incomplete. Email and password are required.");
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}.", loginDto.Email);
                    return this.ApiUnauthorized("Invalid email or password.");
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed: Account for email {Email} is deactivated.", loginDto.Email);
                    return this.ApiUnauthorized("Account is deactivated. Please contact support.");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Login failed: Invalid password for email {Email}.", loginDto.Email);
                    return this.ApiUnauthorized("Invalid email or password.");
                }

                user.LastActivity = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update LastActivity for user {UserId} during login. Errors: {@Errors}", user.Id, updateResult.Errors.Select(e => e.Description));
                    // Continue with login despite this, but log it as an issue.
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);

                // Set HttpOnly cookie for web clients
                Response.Cookies.Append(
                    "auth_token",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(1), // TODO: Consider making cookie lifetime configurable
                        Path = "/"
                    });

                await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                var userDto = await _userService.GetUserByIdAsync(user.Id);

                var tokenResponse = new TokenResponseDto
                {
                    Token = token, // Token is also returned in body for non-browser clients
                    User = userDto
                };
                _logger.LogInformation("API Login SUCCESS for email: {Email}, UserID: {UserId}", loginDto.Email, user.Id);
                return this.ApiOk(tokenResponse, "Login successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user login for email {Email}", loginDto.Email);
                return this.ApiInternalError("An unexpected error occurred during login.", ex);
            }
        }

        /// <summary>
        /// Initiates the password reset process for a user.
        /// </summary>
        /// <param name="forgotPasswordDto">The DTO containing the user's email.</param>
        /// <returns>
        /// A 200 OK response indicating that if the email is registered, instructions will be sent.
        /// This is done to prevent email enumeration.
        /// </returns>
        /// <response code="200">Password reset initiated (or appears to be, to prevent email enumeration).</response>
        /// <response code="400">If the email is not provided.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] // Returns a non-generic ApiResponse with a message
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            _logger.LogInformation("API ForgotPassword attempt for email: {Email}", forgotPasswordDto?.Email ?? "N/A");
            if (forgotPasswordDto == null || string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
            {
                return this.ApiBadRequest("Email is required.");
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);

                // Do not reveal if the user/email exists or is active for security reasons (prevents email enumeration).
                if (user == null || !user.IsActive)
                {
                    _logger.LogInformation("Password reset requested for non-existent or inactive user: {Email}. Sending generic success response.", forgotPasswordDto.Email);
                    // Always return a generic success message to prevent attackers from guessing valid emails.
                    return this.ApiOk("If your email is registered and active, you will receive password reset instructions.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                _logger.LogInformation("Password reset token generated for user {Email} (token not logged).", forgotPasswordDto.Email);

                // TODO: Implement email sending service to send the password reset link/token.
                // Example: await _emailSender.SendPasswordResetEmailAsync(user.Email, token, callbackUrl);
                // The callbackUrl should point to your frontend page that handles password reset.

                _logger.LogInformation("API ForgotPassword SUCCESS (token generated) for email: {Email}", forgotPasswordDto.Email);
                return this.ApiOk("If your email is registered and active, you will receive password reset instructions.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password reset request for email {Email}", forgotPasswordDto.Email);
                return this.ApiInternalError("An error occurred while processing your password reset request.", ex);
            }
        }
/*
        /// <summary>
        /// Resets the user's password using a valid reset token.
        /// </summary>
        /// <param name="resetPasswordDto">The DTO containing email, reset token, and new password.</param>
        /// <returns>
        /// A 200 OK response on successful password reset, or an error response.
        /// </returns>
        /// <response code="200">Password reset successfully.</response>
        /// <response code="400">If data is invalid, or token is invalid/expired.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] // Returns a non-generic ApiResponse with a message
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            _logger.LogInformation("API ResetPassword attempt for email: {Email}", resetPasswordDto?.Email ?? "N/A");
            if (resetPasswordDto == null ||
                string.IsNullOrWhiteSpace(resetPasswordDto.Email) ||
                string.IsNullOrWhiteSpace(resetPasswordDto.Token) ||
                string.IsNullOrWhiteSpace(resetPasswordDto.NewPassword) ||
                string.IsNullOrWhiteSpace(resetPasswordDto.ConfirmNewPassword))
            {
                return this.ApiBadRequest("Email, token, new password, and confirm new password are required.");
            }

            if (resetPasswordDto.NewPassword != resetPasswordDto.ConfirmNewPassword)
            {
                return this.ApiBadRequest("New passwords do not match.");
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
                if (user == null)
                {
                    // Do not reveal that the user doesn't exist.
                    // This helps prevent attackers from confirming valid emails.
                    _logger.LogWarning("Password reset failed: User not found for email {Email} (or token invalid).", resetPasswordDto.Email);
                    return this.ApiBadRequest("Password reset failed. The link may be invalid or expired.");
                }

                var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Password reset failed for user {Email}. Errors: {Errors}",
                        resetPasswordDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    // Provide a generic error message.
                    return this.ApiBadRequest("Password reset failed. The link may be invalid or expired, or the new password does not meet requirements.");
                }

                user.LastActivity = DateTime.UtcNow; // Optionally update last activity.
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("API ResetPassword SUCCESS for email: {Email}", resetPasswordDto.Email);
                return this.ApiOk("Your password has been reset successfully. You can now log in with your new password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password reset for email {Email}", resetPasswordDto.Email);
                return this.ApiInternalError("An unexpected error occurred while resetting your password.", ex);
            }
        }*/
    }
}
