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
            {
                try
                {
                    var user = new User
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CreatedDate = DateTime.UtcNow,
                        LastActivity = DateTime.UtcNow,
                        IsActive = true
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password.");

                        // Generate JWT token for API access
                        var token = await _jwtTokenService.GenerateTokenAsync(user);
                        
                        // Create user session with token
                        await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
                        
                        // Store token in a cookie for JavaScript access
                        Response.Cookies.Append(
                            "jwt_token", 
                            token,
                            new CookieOptions
                            {
                                HttpOnly = false, // Ensure JavaScript can access
                                Secure = Request.IsHttps, 
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTime.Now.AddDays(7),
                                Path = "/"
                            });

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User was logged in after registration.");

                        return RedirectToLocal(returnUrl);
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration");
                    ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
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
    
    if (!ModelState.IsValid)
    {
        return View(model);
    }
    
    try
    {
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in successfully: {Email}", model.Email);
    
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogError("Unable to find user after successful login: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
                return View(model);
            }

            // Generate JWT token
            var token = await _jwtTokenService.GenerateTokenAsync(user);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to generate JWT token for user: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
                return View(model);
            }
    
            // Store token in a cookie
            Response.Cookies.Append(
                "jwt_token",
                token,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.Now.AddDays(7),
                    Path = "/"
                });
            
            _logger.LogInformation("JWT token cookie set for user: {Email}", model.Email);
    
            // Update user's LastActivity
            user.LastActivity = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            
            // Create user session
            await _sessionService.CreateSessionAsync(user, token, null, HttpContext);
            
            // Return the SetTokenAndRedirect view with token data
            TempData["JwtToken"] = token;
            TempData["ReturnUrl"] = returnUrl ?? "/";
            return View("SetTokenAndRedirect");
        }
        
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {Email}", model.Email);
            return RedirectToAction(nameof(Lockout));
        }
        
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Exception during login for email: {Email}", model.Email);
        ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
        return View(model);
    }
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
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
    
}