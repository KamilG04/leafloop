using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models; 
using LeafLoop.Repositories.Interfaces; 
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces; 
using LeafLoop.ViewModels.Profile; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;


namespace LeafLoop.Controllers
{
    [Authorize] // Entire controller requires authorization
    public class ProfileController : BaseController // Assuming BaseController provides IUnitOfWork
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly IItemService _itemService;
        // private readonly IUnitOfWork _unitOfWork; // Already available via BaseController property
        private readonly IMapper _mapper;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUnitOfWork unitOfWork, // Still needed for BaseController constructor
            UserManager<User> userManager,
            IUserService userService,
            IItemService itemService,
            IMapper mapper,
            ILogger<ProfileController> logger)
            : base(unitOfWork) // Pass UoW to base
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Profile or /Profile/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ProfileViewModel viewModel = new ProfileViewModel(); // Initialize ViewModel at the start

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    _logger.LogError("Profile Index: Could not identify user ID from claims.");
                    return Challenge(); // Or return an appropriate error view/redirect
                }

                _logger.LogInformation("Profile Index: Attempting to load profile for UserID: {UserId}", userId);

                // 1. Fetch user details via service
                var userDetailsDto = await _userService.GetUserWithDetailsAsync(userId);
                if (userDetailsDto == null)
                {
                    _logger.LogWarning("Profile Index: UserWithDetailsDto not found for UserID: {UserId}", userId);
                    return NotFound($"User profile with ID {userId} not found."); // Consider a user-friendly 'Not Found' view
                }
                _mapper.Map(userDetailsDto, viewModel); // Map DTO to ViewModel

                // 2. Fetch recent items via service
                var recentItemsDto = await _itemService.GetRecentItemsByUserAsync(userId, 5); // Example: Get 5 recent items
                viewModel.RecentItems = recentItemsDto?.ToList() ?? new List<ItemDto>(); // Assign to ViewModel

                // 3. Fetch counts
                // TODO: Consider moving the logic for calculating TotalItemsCount and TotalTransactionsCount
                // into the IUserService or a dedicated statistics service to keep data access logic out of the controller,
                // improving separation of concerns. For now, direct UoW access is used for simplicity / geyness 900 lines apis XD GREAT WORK
                viewModel.TotalItemsCount = await _unitOfWork.Items.CountAsync(i => i.UserId == userId);
                int sellingCount = await _unitOfWork.Transactions.CountAsync(t => t.SellerId == userId && t.Status == TransactionStatus.Completed);
                int buyingCount = await _unitOfWork.Transactions.CountAsync(t => t.BuyerId == userId && t.Status == TransactionStatus.Completed);
                viewModel.TotalTransactionsCount = sellingCount + buyingCount;

                _logger.LogInformation("Profile Index: Successfully loaded data for UserID: {UserId}", userId);

                // 4. Pass ViewModel to the view
                return View(viewModel);
            }
            catch (Exception ex)
            {
                var userIdForErrorLog = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                _logger.LogError(ex, "Error occurred while loading profile for UserID: {UserId}", userIdForErrorLog);
                return View("Error", new ErrorViewModel { Message = "An error occurred while loading your profile. Please try again later." });
            }
        }

        // GET: /Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                _logger.LogError("Profile Edit (GET): Could not identify user ID from claims.");
                return Challenge();
            }

            _logger.LogInformation("Serving Edit Profile view for UserID: {UserId}", userId);
            ViewBag.UserId = userId; // Pass UserId for rekt 

            return View(); // Returns Views/Profile/Edit.cshtml
        }

        // Placeholder for the missing POST action, CURRENTLY IN API 
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
             // TODO: Implement profile update logic here:
             // 1. Validate ModelState.
             // 2. Get current userId.
             // 3. Map ProfileEditViewModel to a suitable DTO or User object.
             // 4. Call _userService.UpdateProfileAsync(userId, dto/user).
             // 5. Handle success: Redirect to Index, show success message.
             // 6. Handle failure: Return View(model) with errors, log issue.
             throw new NotImplementedException();
        }
        */
    }
}