using LeafLoop.Models;
using LeafLoop.Models.API; // Upewnij się, że ta przestrzeń nazw istnieje i zawiera ApiResponse
using LeafLoop.Services.DTOs;
using LeafLoop.Services.DTOs.Auth;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Potrzebne dla CookieOptions i StatusCodes

namespace LeafLoop.Api;

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

    // --- Plan Naprawczy: Poprawa Bezpieczeństwa Ciasteczek ---
    // Endpoint dla aplikacji SPA potrzebujących tokenu (jeśli ciasteczko HttpOnly nie wystarcza)
    [HttpGet("token")] // Trasa będzie /api/Auth/token
    [Authorize(Policy = "ApiAuthPolicy")] // Dostępne tylko dla uwierzytelnionych użytkowników
    public IActionResult GetTokenForSpa()
    {
        // Token został już zwalidowany przez atrybut [Authorize]
        // i middleware JwtBearer (czytający z ciasteczka 'auth_token').
        // Najprostszy sposób na odzyskanie stringu tokena to odczytanie go ponownie z ciasteczka żądania.
        var tokenString = HttpContext.Request.Cookies["auth_token"];

        if (string.IsNullOrEmpty(tokenString))
        {
            // Ten przypadek nie powinien wystąpić, jeśli [Authorize] zadziałało poprawnie na podstawie ciasteczka.
            _logger.LogWarning(
                "Użytkownik jest autoryzowany, ale ciasteczko 'auth_token' nie zostało znalezione w GetTokenForSpa.");
            // Użyj swojej standardowej metody odpowiedzi API, np. this.ApiNotFound lub podobnej
            return StatusCode(StatusCodes.Status404NotFound,
                new { Message = "Token not found in cookie, though user is authorized." });
        }

        // Zwróć token. Użyj swojej standardowej metody odpowiedzi API.
        // Zakładając, że masz metodę this.ApiOk(dane, wiadomosc)
        // Jeśli nie, użyj return Ok(new { token = tokenString });
        return Ok(new { token = tokenString }); // Prosta odpowiedź, dostosuj do swojego ApiResponse
    }
    // --- Koniec: Poprawa Bezpieczeństwa Ciasteczek (Endpoint dla SPA) ---

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
    {
        if (registrationDto == null || string.IsNullOrWhiteSpace(registrationDto.Email) ||
            string.IsNullOrWhiteSpace(registrationDto.Password))
            // Użyj swojej standardowej metody odpowiedzi API dla BadRequest
            return StatusCode(StatusCodes.Status400BadRequest, new { Message = "Registration data is incomplete." });

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(registrationDto.Email);
            if (existingUser != null)
                // Użyj swojej standardowej metody odpowiedzi API dla Conflict
                return StatusCode(StatusCodes.Status409Conflict, new { Message = "Email is already registered." });

            if (registrationDto.Password != registrationDto.ConfirmPassword)
                return StatusCode(StatusCodes.Status400BadRequest, new { Message = "Passwords do not match." });

            var userId = await _userService.RegisterUserAsync(registrationDto);
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                _logger.LogError("User not found immediately after registration. UserID: {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to retrieve user after registration." });
            }

            var token = await _jwtTokenService.GenerateTokenAsync(user);

            // --- Plan Naprawczy: Poprawa Bezpieczeństwa Ciasteczek (dla API register) ---
            // Ustawianie bezpiecznego ciasteczka HttpOnly
            Response.Cookies.Append(
                "auth_token", // Nazwa ciasteczka zgodna z planem
                token,
                new CookieOptions
                {
                    HttpOnly = true, // Zabezpieczenie przed dostępem przez JavaScript
                    Secure = Request.IsHttps, // Wymagaj HTTPS (w produkcji zawsze true)
                    SameSite = SameSiteMode.Strict, // Surowsza polityka same-site (zgodnie z planem)
                    // Jeśli wystąpią problemy, można rozważyć SameSiteMode.Lax
                    Expires = DateTime.UtcNow.AddHours(1), // Krótszy okres ważności ciasteczka (1 godzina)
                    Path = "/" // Ciasteczko dostępne w całej domenie
                });
            // --- Koniec: Poprawa Bezpieczeństwa Ciasteczek ---

            await _sessionService.CreateSessionAsync(user, token, null,
                HttpContext); // Przekaż HttpContext, jeśli serwis go potrzebuje
            var userDto = await _userService.GetUserByIdAsync(userId);

            var tokenResponse = new TokenResponseDto
            {
                Token = token, // Nadal zwracamy token w ciele odpowiedzi dla klientów nie-przeglądarkowych (np. mobilnych)
                User = userDto
            };

            // Użyj swojej standardowej metody odpowiedzi API dla Ok
            return Ok(tokenResponse); // Zakładam, że this.ApiOk to robi
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user registration for email {Email}", registrationDto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred during registration. Please try again later." });
        }
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Email) ||
            string.IsNullOrWhiteSpace(loginDto.Password))
            return StatusCode(StatusCodes.Status400BadRequest, new { Message = "Login data is incomplete." });

        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
                return StatusCode(StatusCodes.Status401Unauthorized, new { Message = "Invalid email or password." });

            if (!user.IsActive)
                return StatusCode(StatusCodes.Status401Unauthorized,
                    new { Message = "Account is deactivated. Please contact support." });

            var result =
                await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password,
                    false); // lockoutOnFailure: false jest w Twoim kodzie
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for email {Email}", loginDto.Email);
                return StatusCode(StatusCodes.Status401Unauthorized, new { Message = "Invalid email or password." });
            }

            user.LastActivity = DateTime.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                _logger.LogError("Failed to update LastActivity for user {UserId}. Errors: {@Errors}", user.Id,
                    updateResult.Errors);

            var token = await _jwtTokenService.GenerateTokenAsync(user);

            // --- Plan Naprawczy: Poprawa Bezpieczeństwa Ciasteczek (dla API login) ---
            // Ustawianie bezpiecznego ciasteczka HttpOnly
            Response.Cookies.Append(
                "auth_token",
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode
                        .Strict, // Zgodnie z planem. Rozważ .Lax jeśli są problemy z niektórymi przepływami.
                    Expires = DateTime.UtcNow.AddHours(1), // Ciasteczko ważne 1 godzinę
                    Path = "/"
                });
            // --- Koniec: Poprawa Bezpieczeństwa Ciasteczek ---

            await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
            var userDto = await _userService.GetUserByIdAsync(user.Id);

            var tokenResponse = new TokenResponseDto
            {
                Token = token, // Nadal zwracamy token w ciele dla klientów nie-przeglądarkowych
                User = userDto
            };

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user login for email {Email}", loginDto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred during login. Please try again later." });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        if (forgotPasswordDto == null || string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
            return StatusCode(StatusCodes.Status400BadRequest, new { Message = "Email is required." });

        try
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);

            // Zgodnie z praktyką, nie ujawniaj, czy email istnieje
            if (user == null || !user.IsActive)
            {
                _logger.LogInformation("Password reset requested for non-existent or inactive user: {Email}",
                    forgotPasswordDto.Email);
                return Ok(new
                {
                    Message = "If your email is registered and active, you will receive password reset instructions."
                });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            _logger.LogInformation("Password reset token generated for user {Email}",
                forgotPasswordDto.Email); // Nie loguj samego tokenu

            // TODO: Wyślij email z tokenem resetującym hasło
            // await _emailSender.SendPasswordResetEmailAsync(user.Email, token);

            return Ok(new
                { Message = "If your email is registered and active, you will receive password reset instructions." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset request for email {Email}",
                forgotPasswordDto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred. Please try again later." });
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
            return StatusCode(StatusCodes.Status400BadRequest, new { Message = "All fields are required." });

        try
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
                // Nie ujawniaj informacji, czy użytkownik istnieje
                return StatusCode(StatusCodes.Status400BadRequest,
                    new { Message = "Password reset failed. The link may be invalid or expired." });

            var result =
                await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Password reset failed for user {Email}. Errors: {Errors}",
                    resetPasswordDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return StatusCode(StatusCodes.Status400BadRequest,
                    new { Message = "Password reset failed. The link may be invalid or expired." });
            }

            user.LastActivity = DateTime.UtcNow; // Opcjonalnie, jeśli chcesz to śledzić
            await _userManager.UpdateAsync(user);

            return Ok(new
                { Message = "Your password has been reset successfully. You can now log in with your new password." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset for email {Email}", resetPasswordDto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred. Please try again later." });
        }
    }
}