using System;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces; 
using LeafLoop.Services.Interfaces;
using LeafLoop.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserSessionService _sessionService;

        public AccountController(
            IUnitOfWork unitOfWork, 
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IJwtTokenService jwtTokenService,
            IUserSessionService sessionService,
            ILogger<AccountController> logger)
            : base(unitOfWork) // Pass unitOfWork to the base constructor
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _sessionService = sessionService;
            _logger = logger;
            // TODO: Verify if IUnitOfWork is strictly necessary for the BaseController or if its responsibilities
            // could be streamlined, especially if not all derived controllers make direct use of it via base.
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
                    // TODO: Ensure RegisterViewModel explicitly defines FirstName and LastName,
                    // or handle cases where they might be optional/missing more gracefully.
                    var user = new User
                    {
                        UserName = model.Email, // Standard practice to set UserName to Email.
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CreatedDate = DateTime.UtcNow,
                        LastActivity = DateTime.UtcNow,
                        IsActive = true // Defaulting user to active.
                        // TODO: Define a clear default value or initialization strategy for User.EcoScore
                        // if it's not explicitly set during registration and doesn't have a database default.
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password for email: {Email}", model.Email);
                        // TODO: Consider making the default role ("User") configurable or
                        // based on specific registration logic rather than hardcoding.
                        await _userManager.AddToRoleAsync(user, "User");
                        _logger.LogInformation("Assigned 'User' role to new user: {Email}", model.Email);

                        var token = await _jwtTokenService.GenerateTokenAsync(user);

                        if (string.IsNullOrEmpty(token))
                        {
                            _logger.LogError("Failed to generate JWT token for newly registered user: {Email}", model.Email);
                            // TODO: Localize user-facing error messages instead of using hardcoded string. id rather stop using that viewmodel
                            ModelState.AddModelError(string.Empty, "An error occurred while generating the session. Please try logging in manually.");
                            return View(model);
                        }
                        
                        _logger.LogInformation("Token passed to TempData (Register) for user {Email}. Token (first 10 chars): {TokenStart}", model.Email, token.Substring(0, Math.Min(token.Length, 10)));
                        // TODO: Evaluate the necessity of passing the JWT token via TempData to the SetTokenAndRedirect view,
                        // especially since an HttpOnly 'auth_token' cookie is also set.
                        // The client-side script in SetTokenAndRedirect.cshtml should be reviewed to understand how it consumes this.
                        TempData["JwtToken"] = token;
                        TempData["ReturnUrl"] = returnUrl ?? "/";

                        Response.Cookies.Append(
                            "auth_token",
                            token,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = Request.IsHttps, // TODO: Confirm behavior in development (non-HTTPS). Should be conditionally true.
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.UtcNow.AddHours(1),
                                Path = "/"
                            });
                        _logger.LogInformation("HttpOnly 'auth_token' cookie set for registered user: {Email}", model.Email);

                        // TODO: Clarify the purpose of the 'null' argument passed to _sessionService.CreateSessionAsync
                        // and document its expected behavior or replace it with a more explicit value if applicable.
                        await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User was signed in via MVC session after registration: {Email}", model.Email);
                        
                        // The SetTokenAndRedirect view is expected to handle client-side token storage (e.g., localStorage) and redirection.
                        // TODO: Ensure the SetTokenAndRedirect.cshtml view correctly handles the token (if needed from TempData) and the return URL.
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
                    // TODO: Localize this generic error message.
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                }
            }
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
                    // TODO: Localize this error message.
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
                if (!user.IsActive)
                {
                    // TODO: Localize this error message.
                    ModelState.AddModelError(string.Empty, "Account is inactive. Please contact support.");
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
                        // TODO: Localize this error message.
                        ModelState.AddModelError(string.Empty, "An error occurred while generating the session. Please try again.");
                        return View(model);
                    }

                    _logger.LogInformation("Token passed to TempData (Login) for user {Email}. Token (first 10 chars): {TokenStart}", model.Email, token.Substring(0, Math.Min(token.Length, 10)));
                    // TODO: Same review comment as in Register regarding TempData["JwtToken"] and SetTokenAndRedirect.
                    TempData["JwtToken"] = token;
                    TempData["ReturnUrl"] = returnUrl ?? "/";

                    Response.Cookies.Append(
                        "auth_token",
                        token,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = Request.IsHttps, // TODO: Confirm behavior in development (non-HTTPS).
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddHours(1),
                            Path = "/"
                        });
                    _logger.LogInformation("HttpOnly 'auth_token' cookie set for user: {Email}", model.Email);

                    user.LastActivity = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    // TODO: Same review comment for _sessionService.CreateSessionAsync 'null' argument as in Register.
                    await _sessionService.CreateSessionAsync(user, token, null, HttpContext);

                    // TODO: Confirm SetTokenAndRedirect.cshtml handles login flow correctly.
                    return View("SetTokenAndRedirect");
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out: {Email}", model.Email);
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    // TODO: Localize this error message.
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred during login for email {Email}", model.Email);
                // TODO: Localize this generic error message.
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity.Name;

            await _signInManager.SignOutAsync(); // Clears the ASP.NET Core Identity cookie.

            if (Request.Cookies.ContainsKey("auth_token"))
            {
                Response.Cookies.Delete("auth_token", new CookieOptions { Path = "/" });
                _logger.LogInformation("HttpOnly 'auth_token' cookie deleted by server for user (if known): {UserName}.", userName ?? "Unknown");
            }
            
            // TODO: Investigate if the non-HttpOnly 'jwt_token' cookie is still actively used
            // or if this deletion logic is for a legacy scenario. If obsolete, consider removing.
            if (Request.Cookies.ContainsKey("jwt_token"))
            {
                 Response.Cookies.Delete("jwt_token", new CookieOptions { Path = "/" });
                _logger.LogInformation("Non-HttpOnly 'jwt_token' cookie deleted by server for user (if known): {UserName}.", userName ?? "Unknown");
            }

            _logger.LogInformation("User logged out: {UserName}", userName ?? "Unknown");
            // TODO: Ensure that client-side scripts (e.g., auth.js) robustly clear any tokens
            // stored in localStorage or sessionStorage upon redirection to Home/Index after logout.
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

        // The RedirectToLocal helper method was removed.
        // TODO: If this helper method is used elsewhere or its functionality is needed,
        // it should be reinstated or its logic incorporated where required.
        // Currently, redirection logic seems handled by SetTokenAndRedirect view and standard MVC redirects.
    }
}