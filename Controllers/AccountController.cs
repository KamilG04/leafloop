using System;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces; // Potrzebne dla IUnitOfWork w BaseController
using LeafLoop.Services.Interfaces;
using LeafLoop.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Potrzebne dla CookieOptions
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    public class AccountController : BaseController // Zakładam, że BaseController przyjmuje IUnitOfWork
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserSessionService _sessionService;

        public AccountController(
            IUnitOfWork unitOfWork, // Dodane, jeśli BaseController tego wymaga
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IJwtTokenService jwtTokenService,
            IUserSessionService sessionService,
            ILogger<AccountController> logger)
            : base(unitOfWork) // Przekaż unitOfWork do konstruktora bazowego
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new User
                    {
                        UserName = model.Email, // Upewnij się, że UserName jest ustawiany
                        Email = model.Email,
                        FirstName = model.FirstName, // Zakładając, że RegisterViewModel ma te pola
                        LastName = model.LastName,   // Zakładając, że RegisterViewModel ma te pola
                        CreatedDate = DateTime.UtcNow,
                        LastActivity = DateTime.UtcNow,
                        IsActive = true // Domyślnie aktywuj użytkownika
                        // EcoScore można ustawić na wartość domyślną lub pominąć, jeśli ma default w DB
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password for email: {Email}", model.Email);
                        await _userManager.AddToRoleAsync(user, "User");
                        _logger.LogInformation("Assigned 'User' role to new user: {Email}", model.Email);

                        var token = await _jwtTokenService.GenerateTokenAsync(user);

                        if (string.IsNullOrEmpty(token))
                        {
                            _logger.LogError("Failed to generate JWT token for newly registered user: {Email}", model.Email);
                            ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas generowania sesji. Spróbuj zalogować się ręcznie.");
                            return View(model);
                        }
                        
                        _logger.LogInformation("Token przekazywany do TempData (Register) dla user {Email}. Token (pierwsze 10 znaków): {TokenStart}", model.Email, token.Substring(0, Math.Min(token.Length, 10)));
                        TempData["JwtToken"] = token;
                        TempData["ReturnUrl"] = returnUrl ?? "/";


                        // Ustawienie ciasteczka HttpOnly 'auth_token'
                        Response.Cookies.Append(
                            "auth_token",
                            token,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = Request.IsHttps,
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.UtcNow.AddHours(1), // Czas życia ciasteczka
                                Path = "/"
                            });
                        _logger.LogInformation("HttpOnly 'auth_token' cookie set for registered user: {Email}", model.Email);

                        await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                        await _signInManager.SignInAsync(user, isPersistent: false); // Zaloguj użytkownika w sesji MVC
                        _logger.LogInformation("User was signed in via MVC session after registration: {Email}", model.Email);
                        
                        // Zamiast RedirectToLocal, użyj SetTokenAndRedirect, aby JS mógł zapisać token
                        return View("SetTokenAndRedirect");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An exception occurred during user registration for email {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.");
                }
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Nieprawidłowy login lub hasło.");
                    return View(model);
                }
                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Konto jest nieaktywne. Skontaktuj się z pomocą techniczną.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in successfully: {Email}", model.Email);
                    var token = await _jwtTokenService.GenerateTokenAsync(user);

                    if (string.IsNullOrEmpty(token))
                    {
                        _logger.LogError("Failed to generate JWT token for user: {Email}", model.Email);
                        ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas generowania sesji. Spróbuj ponownie.");
                        return View(model);
                    }

                    // DODANE LOGOWANIE TOKENU PRZEKAZYWANEGO DO TEMPDATA
                    _logger.LogInformation("Token przekazywany do TempData (Login) dla user {Email}. Token (pierwsze 10 znaków): {TokenStart}", model.Email, token.Substring(0, Math.Min(token.Length, 10)));
                    TempData["JwtToken"] = token;
                    TempData["ReturnUrl"] = returnUrl ?? "/";

                    // Ustawienie ciasteczka HttpOnly 'auth_token'
                    Response.Cookies.Append(
                        "auth_token",
                        token,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = Request.IsHttps,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddHours(1), // Czas życia ciasteczka
                            Path = "/"
                        });
                    _logger.LogInformation("HttpOnly 'auth_token' cookie set for user: {Email}", model.Email);

                    user.LastActivity = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    await _sessionService.CreateSessionAsync(user, token, null, HttpContext);

                    return View("SetTokenAndRedirect");
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out: {Email}", model.Email);
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Nieprawidłowy login lub hasło.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred during login for email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity.Name; // Pobierz nazwę użytkownika przed wylogowaniem dla logu

            await _signInManager.SignOutAsync(); // Usuwa ciasteczko Identity.Application

            // JAWNE USUNIĘCIE CIASTECZKA 'auth_token' (HttpOnly)
            if (Request.Cookies.ContainsKey("auth_token"))
            {
                Response.Cookies.Delete("auth_token", new CookieOptions { Path = "/" });
                _logger.LogInformation("HttpOnly 'auth_token' cookie deleted by server for user (if known): {UserName}.", userName ?? "Unknown");
            }
            
            // Opcjonalnie, jeśli kiedykolwiek używałeś serwerowo ustawianego 'jwt_token' (nie-HttpOnly)
            if (Request.Cookies.ContainsKey("jwt_token"))
            {
                 Response.Cookies.Delete("jwt_token", new CookieOptions { Path = "/" });
                _logger.LogInformation("Non-HttpOnly 'jwt_token' cookie deleted by server for user (if known): {UserName}.", userName ?? "Unknown");
            }

            _logger.LogInformation("User logged out: {UserName}", userName ?? "Unknown");
            // Przekierowanie na stronę główną, gdzie JS (np. w auth.js) powinien też wyczyścić localStorage
            // jeśli jest wywoływany przez np. kliknięcie przycisku "Wyloguj" na stronie.
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Usunięto RedirectToLocal, ponieważ SetTokenAndRedirect obsługuje przekierowanie
        // Jeśli potrzebujesz tej funkcji gdzie indziej, możesz ją przywrócić.
    }
}
