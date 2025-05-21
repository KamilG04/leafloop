
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
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    [Authorize] // Cały kontroler wymaga autoryzacji
    [Route("Profile")] // Definiujemy bazowy route dla kontrolera
    public class ProfileController : BaseController
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly IItemService _itemService;
        private readonly IMapper _mapper;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IUserService userService,
            IItemService itemService,
            IMapper mapper,
            ILogger<ProfileController> logger)
            : base(unitOfWork)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Obsłuży:
        // GET: /Profile (wyświetli profil zalogowanego użytkownika)
        // GET: /Profile/Index (wyświetli profil zalogowanego użytkownika)
        // GET: /Profile/Index/{id} (wyświetli profil użytkownika o danym id)
        // GET: /Profile/{id} (wyświetli profil użytkownika o danym id - jeśli nie ma konfliktu z inną akcją domyślną)
        [HttpGet("Index/{id:int?}")] // np. /Profile/Index/5 lub /Profile/Index
        [HttpGet("{id:int?}")]      // np. /Profile/5 (jeśli Index jest domyślną akcją dla tego kontrolera)
                                     // Jeśli domyślna akcja to nie Index, ten route może być problematyczny bez "Index/"
        [HttpGet("")]                // Dla /Profile (bez ID)
        public async Task<IActionResult> Index(int? id)
        {
            _logger.LogInformation("ProfileController.Index triggered. Requested ID from route: {RouteId}", id?.ToString() ?? "Not provided");

            int userIdToDisplay;
            User authenticatedUser = await _userManager.GetUserAsync(User); // Pobierz zalogowanego użytkownika

            if (authenticatedUser == null)
            {
                // To nie powinno się zdarzyć przy [Authorize] na kontrolerze, chyba że sesja/cookie wygasło tuż przed.
                _logger.LogWarning("ProfileController.Index: User is considered authenticated by [Authorize], but GetUserAsync returned null. Challenging.");
                return Challenge(); // Wymuś ponowne logowanie
            }
            _logger.LogInformation("ProfileController.Index: Authenticated UserID is {AuthenticatedUserId}", authenticatedUser.Id);

            if (id.HasValue && id.Value > 0)
            {
                // Żądanie wyświetlenia profilu o konkretnym ID
                userIdToDisplay = id.Value;
                _logger.LogInformation("ProfileController.Index: Will display profile for UserID from route: {UserIdToDisplay}", userIdToDisplay);
            }
            else
            {
                // Brak ID w URL, wyświetlamy profil zalogowanego użytkownika
                userIdToDisplay = authenticatedUser.Id;
                _logger.LogInformation("ProfileController.Index: No ID in route. Displaying profile for logged-in UserID: {UserIdToDisplay}", userIdToDisplay);
            }

            try
            {
                var userDetailsDto = await _userService.GetUserWithDetailsAsync(userIdToDisplay);
                if (userDetailsDto == null)
                {
                    _logger.LogWarning("ProfileController.Index: UserDetailsDto not found for UserID: {UserIdToDisplay}", userIdToDisplay);
                    return NotFound($"User profile with ID {userIdToDisplay} not found.");
                }

                var viewModel = _mapper.Map<ProfileViewModel>(userDetailsDto);
                viewModel.UserId = userDetailsDto.Id; // Upewnij się, że ID jest ustawione
                viewModel.IsCurrentUserProfile = (userIdToDisplay == authenticatedUser.Id);
                // viewModel.SearchRadius = userDetailsDto.SearchRadius; // AutoMapper powinien to zrobić, jeśli ProfileViewModel ma SearchRadius

                 _logger.LogInformation("ProfileController.Index: ViewModel prepared for UserID {UserIdToDisplay}. IsCurrentUserProfile: {IsCurrent}. SearchRadius from DTO: {SearchRadius}", 
                    viewModel.UserId, viewModel.IsCurrentUserProfile, userDetailsDto.SearchRadius);


                var recentItemsDto = await _itemService.GetRecentItemsByUserAsync(userIdToDisplay, 5);
                viewModel.RecentItems = _mapper.Map<List<ItemDto>>(recentItemsDto);

                viewModel.TotalItemsCount = await _unitOfWork.Items.CountAsync(i => i.UserId == userIdToDisplay && i.IsAvailable);
                int sellingCount = await _unitOfWork.Transactions.CountAsync(t => t.SellerId == userIdToDisplay && t.Status == TransactionStatus.Completed);
                int buyingCount = await _unitOfWork.Transactions.CountAsync(t => t.BuyerId == userIdToDisplay && t.Status == TransactionStatus.Completed);
                viewModel.TotalTransactionsCount = sellingCount + buyingCount;

                _logger.LogInformation("ProfileController.Index: Successfully loaded all data for UserID: {UserIdToDisplay}", userIdToDisplay);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProfileController.Index: Error occurred while loading profile for UserID: {UserIdToDisplay}", userIdToDisplay);
                var errorViewModel = new ErrorViewModel
                {
                    Message = "An error occurred while loading the profile. Please try again later."
                };
                return View("Error", errorViewModel);
            }
        }

        // GET: /Profile/Edit
        [HttpGet("Edit")] // Jawnie określona trasa, aby uniknąć konfliktu z Index(id?)
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("ProfileController.Edit (GET): Could not identify authenticated user.");
                return Challenge();
            }

            _logger.LogInformation("ProfileController.Edit (GET): Serving Edit Profile view for UserID: {UserId}", user.Id);

            var userDetailsDto = await _userService.GetUserWithDetailsAsync(user.Id);
            if (userDetailsDto == null)
            {
                _logger.LogWarning("ProfileController.Edit (GET): UserWithDetailsDto not found for UserID: {UserId} for editing.", user.Id);
                return NotFound($"User profile with ID {user.Id} not found for editing.");
            }
            
            // TODO: Zmapuj userDetailsDto na dedykowany ProfileEditViewModel, jeśli go masz
            // var editViewModel = _mapper.Map<ProfileEditViewModel>(userDetailsDto);
            // return View(editViewModel);
            
            ViewBag.UserId = user.Id; // Jeśli widok Edit.cshtml tego oczekuje
            return View();
        }

        /*
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
             // TODO: Implement profile update logic here
             throw new NotImplementedException();
        }
        */
    }
}