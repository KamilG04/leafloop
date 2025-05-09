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

namespace LeafLoop.Controllers;

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
        : base(unitOfWork)
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
            try
            {
                var user = new User
                {
                    /* ... */
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    var token = await _jwtTokenService.GenerateTokenAsync(user);

                    // --- Plan Naprawczy: Poprawa Bezpieczeństwa Ciasteczek ---
                    // Zmiana nazwy dla uniknięcia konfliktów -> "auth_token"
                    // HttpOnly = true, // Zabezpieczenie przed dostępem przez JavaScript
                    // Secure = true, // Tylko przez HTTPS
                    // SameSite = SameSiteMode.Strict, // Surowsza polityka same-site
                    // Expires = DateTime.Now.AddHours(1), // Krótszy okres ważności
                    // Path = "/"
                    Response.Cookies.Append(
                        "auth_token",
                        token,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = Request.IsHttps, // This will be true if your site uses HTTPS
                            SameSite = SameSiteMode.Strict, // Stricter than Lax
                            Expires = DateTime.UtcNow.AddHours(1), // Cookie expiry, 1 hour
                            Path = "/"
                        });

                    await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                    await _signInManager.SignInAsync(user, false);
                    _logger.LogInformation("User was logged in after registration.");
                    return RedirectToLocal(returnUrl);
                }
                // ... (error handling) ...
            }
            catch (Exception ex)
            {
                /* ... */
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


    // In Controllers/AccountController.cs

// In AccountController.cs - Fix the login flow
// In AccountController.cs - Update the Login method
// In Controllers/AccountController.cs - Update the Login method

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in successfully: {Email}", model.Email);
                var user = await _userManager.FindByEmailAsync(model.Email);
                // ... (null check for user) ...

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                if (string.IsNullOrEmpty(token))
                {
                    /* ... error handling ... */
                }

                // --- Plan Naprawczy: Poprawa Bezpieczeństwa Ciasteczek ---
                Response.Cookies.Append(
                    "auth_token",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(1), // Cookie expiry, 1 hour
                        Path = "/"
                    });
                // --- Koniec: Poprawa Bezpieczeństwa Ciasteczek ---

                _logger.LogInformation("JWT token cookie 'auth_token' set for user: {Email}", model.Email);

                user.LastActivity = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                await _sessionService.CreateSessionAsync(user, token, null, HttpContext);

                // The SetTokenAndRedirect view might not be necessary if the token is HttpOnly
                // and the API relies on the cookie being sent by the browser.
                // If it was for JS to pick up the token, that's changed.
                // For now, we keep the redirect logic, assuming it's for page navigation.
                TempData["ReturnUrl"] = returnUrl ?? "/"; // No need to pass JwtToken in TempData if HttpOnly

                // If SetTokenAndRedirect was to make JS grab the token, it's no longer needed for that purpose.
                // Let's assume it's a generic post-login redirect page.
                // If you were using SetTokenAndRedirect to inject the token into a script,
                // that specific part needs rethinking due to HttpOnly = true.
                // For now, let's simplify and assume it redirects.
                // If the view "SetTokenAndRedirect" itself relies on TempData["JwtToken"], 
                // then we might need to adjust that view or remove the need for it if the token is HttpOnly.
                // The plan doesn't mention this view, so we'll stick to the core cookie change.
                // A direct redirect is often simpler after login if no client-side token handling is needed.
                // return RedirectToLocal(returnUrl); // Simpler redirect
                return View("SetTokenAndRedirect"); // Keeping your existing flow for now
            }
            // ... (other conditions: IsLockedOut, ModelState error) ...
        }
        catch (Exception ex)
        {
            /* ... */
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
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

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        else
            return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}